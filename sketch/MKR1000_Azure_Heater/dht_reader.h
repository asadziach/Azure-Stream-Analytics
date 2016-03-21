// Copyright (c) Asad Zia
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifndef DHT_READER_H
#define DHT_READER_H

#ifdef __cplusplus
extern "C" {
#endif

typedef struct _dht_readout{
  float temp;
  float humidity;
} DHT_Readout;

void dht_setup(void);
int dht_read(DHT_Readout* readout);
    

#ifdef __cplusplus
}
#endif

#endif /* DHT_READER_H */

