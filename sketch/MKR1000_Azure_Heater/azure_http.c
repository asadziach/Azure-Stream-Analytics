// Copyright (c) Microsoft. All rights reserved.
// Copyright (c) Asad Zia
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "Arduino.h"

#include <stdlib.h>

#include <stdio.h>
#include <stdint.h>

#ifdef ARDUINO
#include "AzureIoT.h"
#else
#include "serializer.h"
#include "iothub_client_ll.h"
#include "iothubtransporthttp.h"
#include "threadapi.h"
#endif

#include "dht_reader.h"
#include "lcd.h"

// This file is not added to github because every user has different wifi and cloud credentials.
#include "network_credentials.h"

static const char* connectionString = AZURE_CONNECTION_STRING;   //  Create network_credentials.secret and define there.

const long UPLOAD_INTERVAL = 10000;           // Sensor data upload interval (milliseconds)

#define IGNITION_PIN 4

#define SOL_HOLD_PIN 0 
#define SOL_TRIG_PIN 1

#define MAX_TRIES 20
#define FLAME_THRESHHOLD 500

static char* Heater_Status = "OFF";
static int Desired_Temp = 32; 

// Define the Model
BEGIN_NAMESPACE(SmartHeater);

DECLARE_MODEL(Heater,
              WITH_DATA(ascii_char_ptr, deviceid),
              WITH_DATA(int, currenttemp),
              WITH_DATA(int, gassense),
              WITH_DATA(int, flamesense),
              WITH_DATA(int, humidity),
              WITH_DATA(ascii_char_ptr, status),
              WITH_ACTION(TurnHeaterOn),
              WITH_ACTION(TurnHeaterOff, int, reason),
              WITH_ACTION(SetDesiredTemp, int, temperature)
             );

END_NAMESPACE(SmartHeater);

DEFINE_ENUM_STRINGS(IOTHUB_CLIENT_CONFIRMATION_RESULT, IOTHUB_CLIENT_CONFIRMATION_RESULT_VALUES)

EXECUTE_COMMAND_RESULT TurnHeaterOn(Heater* device)
{
  (void)device;
  (void)printf("Turning heater on.\r\n");

   //Turn ON both solinoid MOSFETs for trigger mode
   digitalWrite(SOL_HOLD_PIN, LOW);
   digitalWrite(SOL_TRIG_PIN, LOW);

   int j;
   for(j=0; j< MAX_TRIES; j++){
      int sensorRead = analogRead(A2);
      
      if(sensorRead > FLAME_THRESHHOLD){
         delay(1000);
         for(int i=0; i< 10; i++){ 
          
           digitalWrite(IGNITION_PIN, LOW);   
           delay(5);              
           digitalWrite(IGNITION_PIN, HIGH);    
           delay(5); 
           
         }       
      }else{
        Heater_Status = "ON";
        
           //Turn OFF high current solinoid MOSFETs for hold mode
        digitalWrite(SOL_TRIG_PIN, HIGH);
        break;
      }
  }
  if(j==MAX_TRIES){
     Heater_Status = "ALARM: Ignition failure, turned OFF";
     
     digitalWrite(SOL_HOLD_PIN, HIGH); // Turn solinoid MOSFETs off
     digitalWrite(SOL_TRIG_PIN, HIGH);
  }
  return EXECUTE_COMMAND_SUCCESS;
}

EXECUTE_COMMAND_RESULT TurnHeaterOff(Heater* device, int reason)
{
  (void)device;
  (void)printf("Turning heater off.\r\n");

  digitalWrite(SOL_HOLD_PIN, HIGH); // Turn solinoid MOSFETs off
  digitalWrite(SOL_TRIG_PIN, HIGH);
     
  if(reason==0){
      Heater_Status = "OFF";
  }else{
    Heater_Status = "ALARM: Forced Off";
  }
  return EXECUTE_COMMAND_SUCCESS;
}

EXECUTE_COMMAND_RESULT SetDesiredTemp(Heater* device, int temprature)
{
  (void)device;
  (void)printf("Setting Desired temprature to %d.\r\n", temprature);
  Desired_Temp = temprature;
  return EXECUTE_COMMAND_SUCCESS;
}

void sendCallback(IOTHUB_CLIENT_CONFIRMATION_RESULT result, void* userContextCallback)
{
  int messageTrackingId = (intptr_t)userContextCallback;

  (void)printf("Message Id: %d Received.\r\n", messageTrackingId);

  (void)printf("Result Call Back Called! Result is: %s \r\n", ENUM_TO_STRING(IOTHUB_CLIENT_CONFIRMATION_RESULT, result));
}

static void sendMessage(IOTHUB_CLIENT_LL_HANDLE iotHubClientHandle, const unsigned char* buffer, size_t size)
{
  static unsigned int messageTrackingId;
  IOTHUB_MESSAGE_HANDLE messageHandle = IoTHubMessage_CreateFromByteArray(buffer, size);
  if (messageHandle == NULL)
  {
    printf("unable to create a new IoTHubMessage\r\n");
  }
  else
  {
    if (IoTHubClient_LL_SendEventAsync(iotHubClientHandle, messageHandle, sendCallback, (void*)(uintptr_t)messageTrackingId) != IOTHUB_CLIENT_OK)
    {
      printf("failed to hand over the message to IoTHubClient");
    }
    else
    {
      printf("IoTHubClient accepted the message for delivery\r\n");
    }
    IoTHubMessage_Destroy(messageHandle);
  }
  free((void*)buffer);
  messageTrackingId++;
}

/*this function "links" IoTHub to the serialization library*/
static IOTHUBMESSAGE_DISPOSITION_RESULT IoTHubMessage(IOTHUB_MESSAGE_HANDLE message, void* userContextCallback)
{
  IOTHUBMESSAGE_DISPOSITION_RESULT result;
  const unsigned char* buffer;
  size_t size;
  if (IoTHubMessage_GetByteArray(message, &buffer, &size) != IOTHUB_MESSAGE_OK)
  {
    printf("unable to IoTHubMessage_GetByteArray\r\n");
    result = EXECUTE_COMMAND_ERROR;
  }
  else
  {
    /*buffer is not zero terminated*/
    char* temp = malloc(size + 1);
    if (temp == NULL)
    {
      printf("failed to malloc\r\n");
      result = EXECUTE_COMMAND_ERROR;
    }
    else
    {
      memcpy(temp, buffer, size);
      temp[size] = '\0';
      EXECUTE_COMMAND_RESULT executeCommandResult = EXECUTE_COMMAND(userContextCallback, temp);
      result =
        (executeCommandResult == EXECUTE_COMMAND_ERROR) ? IOTHUBMESSAGE_ABANDONED :
        (executeCommandResult == EXECUTE_COMMAND_SUCCESS) ? IOTHUBMESSAGE_ACCEPTED :
        IOTHUBMESSAGE_REJECTED;
      free(temp);
    }
  }
  return result;
}

void processSensors(IOTHUB_CLIENT_LL_HANDLE iotHubClientHandle, Heater* heaterIns) {
  static unsigned long previousMillis = 0;
  unsigned long currentMillis = millis();

  if (currentMillis - previousMillis >= UPLOAD_INTERVAL) {
    previousMillis = currentMillis;

    DHT_Readout readout;
    dht_read(&readout);

    heaterIns->deviceid = "Heater1";
    heaterIns->currenttemp = readout.temp;
    heaterIns->humidity = readout.humidity;
    heaterIns->gassense = analogRead(A1); //Read Gas value from analog 1;
    heaterIns->flamesense = analogRead(A2); //Read Gas value from analog 2;
    heaterIns->status = Heater_Status;
    {
      unsigned char* destination;
      size_t destinationSize;
      if (SERIALIZE(&destination, &destinationSize, heaterIns->deviceid, heaterIns->currenttemp, heaterIns->gassense, heaterIns->flamesense, heaterIns->humidity, heaterIns->status ) != IOT_AGENT_OK)
      {
        (void)printf("Failed to serialize\r\n");
      }
      else
      {
        IOTHUB_MESSAGE_HANDLE messageHandle = IoTHubMessage_CreateFromByteArray(destination, destinationSize);
        if (messageHandle == NULL)
        {
          printf("unable to create a new IoTHubMessage\r\n");
        }
        else
        {
          if (IoTHubClient_LL_SendEventAsync(iotHubClientHandle, messageHandle, sendCallback, (void*)1) != IOTHUB_CLIENT_OK)
          {
            printf("failed to hand over the message to IoTHubClient");
          }
          else
          {
            printf("IoTHubClient accepted the message for delivery\r\n");
          }

          IoTHubMessage_Destroy(messageHandle);
        }
        printf(destination);
        free(destination);
      }
    }

    //printf("Gas: %d\tFlame: %d\tHumidity: %d\t Temperature: %d *C\r\n", heaterIns->gassense, heaterIns->flamesense, heaterIns->humidity, heaterIns->currenttemp);
    displayRefresh(heaterIns->currenttemp,heaterIns->humidity,Desired_Temp);
    
  }
}

void azure_http_run(void)
{
  if (serializer_init(NULL) != SERIALIZER_OK)
  {
    (void)printf("Failed on serializer_init\r\n");
  }
  else
  {
    IOTHUB_CLIENT_LL_HANDLE iotHubClientHandle = IoTHubClient_LL_CreateFromConnectionString(connectionString, HTTP_Protocol);
    srand((unsigned int)time(NULL));
    int avgTemp = 25;

    if (iotHubClientHandle == NULL)
    {
      (void)printf("Failed on IoTHubClient_LL_Create\r\n");
    }
    else
    {
      unsigned int minimumPollingTime = 4; /*because it can poll "after 4 seconds" polls will happen effectively at ~5 seconds*/
      if (IoTHubClient_LL_SetOption(iotHubClientHandle, "MinimumPollingTime", &minimumPollingTime) != IOTHUB_CLIENT_OK)
      {
        printf("failure to set option \"MinimumPollingTime\"\r\n");
      }

      Heater* heaterIns = CREATE_MODEL_INSTANCE(SmartHeater, Heater);
      if (heaterIns == NULL)
      {
        (void)printf("Failed on CREATE_MODEL_INSTANCE\r\n");
      }
      else
      {
        if (IoTHubClient_LL_SetMessageCallback(iotHubClientHandle, IoTHubMessage, heaterIns) != IOTHUB_CLIENT_OK)
        {
          printf("unable to IoTHubClient_SetMessageCallback\r\n");
        }
        else
        {

          /* wait for commands */
          while (1)
          {
            processSensors(iotHubClientHandle, heaterIns);

            IoTHubClient_LL_DoWork(iotHubClientHandle);
            ThreadAPI_Sleep(100);
          }
        }

        DESTROY_MODEL_INSTANCE(heaterIns);
      }
      IoTHubClient_LL_Destroy(iotHubClientHandle);
    }
    serializer_deinit();
  }
}

