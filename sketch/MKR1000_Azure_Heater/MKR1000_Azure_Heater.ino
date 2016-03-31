// Copyright (c) Arduino. All rights reserved.
// Copyright (c) Asad Zia
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include <WiFi101.h>

#include "azure_http.h"
#include "dht_reader.h"
#include "lcd.h"

// This file is not added to github because every user has different wifi and cloud credentials.
#include "network_credentials.h" 


#define IGNITION_PIN 4

#define SOL_HOLD_PIN 0 
#define SOL_TRIG_PIN 1

char ssid[] = WIFI_SSID;        //  Create network_credentials.secret and define there.
char pass[] = WIFI_PASSWORD;    //  Create network_credentials.secret and define there.

int status = WL_IDLE_STATUS;


void setup() {
  Serial.begin(9600);

   lcd_setup();
   
  // initialize digital pin as an output. MOSFET gate is also connected to this.
   pinMode(IGNITION_PIN, OUTPUT);
   digitalWrite(IGNITION_PIN, HIGH);

  
   pinMode(SOL_HOLD_PIN, OUTPUT);
   digitalWrite(SOL_HOLD_PIN, HIGH); // Connected to inverted buffer

   pinMode(SOL_TRIG_PIN, OUTPUT);
   digitalWrite(SOL_TRIG_PIN, HIGH); // Connected to inverted buffer
  
   dht_setup();              // DHT requires some wait before it can be read. Our
                             // WiFi setup should procide sufficent delay.    
   analogReadResolution(12); //The MKR1000, Zero and the Due have 12-bit ADC


  // check for the presence of the shield :
  if (WiFi.status() == WL_NO_SHIELD) {
    Serial.println("WiFi shield not present");
    // don't continue:
    while (true);
  }

  // attempt to connect to Wifi network:
  while (status != WL_CONNECTED) {
    Serial.print("Attempting to connect to SSID: ");
    Serial.println(ssid);
    // Connect to WPA/WPA2 network. Change this line if using open or WEP network:
    status = WiFi.begin(ssid, pass);

    if (status != WL_CONNECTED) {
      // wait 10 seconds for connection:
      delay(10000);
    }
  }
  Serial.println("Connected to wifi");

  
}

void loop() {
  azure_http_run();
}

