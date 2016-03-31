// Copyright (c) Asad Zia
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifndef LCD_H
#define LCD_H

#ifdef __cplusplus
extern "C" {
#endif

void lcd_setup();
void displayRefresh(int current_temp, int humidity, int desired_temp);    

#ifdef __cplusplus
}
#endif

#endif /* LCD_H */

