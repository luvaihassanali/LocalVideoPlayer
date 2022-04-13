#ifndef UTILS_H
#define UTILS_H

#include <Arduino.h>

const bool DEBUG = true;

extern bool clientConnected;
extern bool esp8266Init;
extern bool soundBarPowerSwitch;
extern bool opticalBluetoothSwitch;

extern int currentState;
extern int button1State;
extern int button2State;
extern int button3State;
extern int redButtonState;
extern int blueButtonState;
extern int joystickButtonState;
extern int joystickMapX;
extern int joystickMapY;
extern int joystickXPos;
extern int joystickYPos;
extern unsigned long currentMillis;
extern String joystickOutput;
const unsigned long KA_TIMEOUT = 4999;

const int RED_LED_PIN = 12;
const int GREEN_LED_PIN = 10;
const int BLUE_LED_PIN = 11;
const int BTN1_PIN = A2;
const int BTN2_PIN = A3;
const int BTN3_PIN = A4;
const int RED_BTN_PIN = 4;
const int BLUE_BTN_PIN = 5;
const int JS_BTN_PIN = 2;
const int JS_RX_PIN = A0;
const int JS_RY_PIN = A1;
const int JS_THRESHOLD = 10;

const uint16_t SAMSUNG_ADDR = 0x707;
const uint8_t SAMSUNG_POWER = 0xE6;
const uint8_t SAMSUNG_HOME = 0x79;
const uint8_t SAMSUNG_BACK = 0x58;
const uint8_t SAMSUNG_ENTER = 0x68;
const uint8_t SAMSUNG_LEFT = 0x65;
const uint8_t SAMSUNG_RIGHT = 0x62;
const uint8_t SAMSUNG_UP = 0x60;
const uint8_t SAMSUNG_DOWN = 0x61;
const uint8_t SAMSUNG_VOL_UP = 0x7;
const uint8_t SAMSUNG_VOL_DOWN = 0xB;
const uint8_t SAMSUNG_STOP = 0x46;

const uint8_t BOMAKER_ADDR = 0x1;
const uint8_t BOMAKER_POWER = 0x10;
const uint8_t BOMAKER_MUTE = 0x17;
const uint8_t BOMAKER_VOL_UP = 0x11;
const uint8_t BOMAKER_VOL_DOWN = 0x8;
const uint8_t BOMAKER_BLUETOOTH = 0x4A;
const uint8_t BOMAKER_OPTICAL = 0x49;

// The following variables are automatically generated using IrScrutinizer 2.3.0 and Bomaker Ondine 1 Soundbar.rmdu (http://www.hifi-remote.com/forums/dload.php?action=file&file_id=25809)
// Prefixes I_ = intro signal and R_ = repeat signal
// Function [kHz] = Hz2kHz(Hz) convert frequency from hertz to kilohertz

typedef uint16_t MICROSECONDS_T;
typedef uint16_t FREQUENCY_T;
inline unsigned HZ_2_KHZ(FREQUENCY_T f) { return f / 1000U; }

const MICROSECONDS_T I_POWER[] PROGMEM = {9024U, 4512U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 39756};
const MICROSECONDS_T R_POWER[] PROGMEM = {9024U, 2256U, 564U, 65535U};
const MICROSECONDS_T I_BT[] PROGMEM = {9024U, 4512U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 564U, 564U, 1692U, 564U, 564U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 564U, 564U, 1692U, 564U, 564U, 564U, 1692U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 564U, 564U, 1692U, 564U, 39756};
const MICROSECONDS_T R_BT[] PROGMEM = {9024U, 2256U, 564U, 65535U};
const MICROSECONDS_T I_OPTICAL[] PROGMEM = {9024U, 4512U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 564U, 564U, 1692U, 564U, 39756};
const MICROSECONDS_T R_OPTICAL[] PROGMEM = {9024U, 2256U, 564U, 65535U};
const MICROSECONDS_T I_UP[] PROGMEM = {9024U, 4512U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 39756};
const MICROSECONDS_T R_UP[] PROGMEM = {9024U, 2256U, 564U, 65535U};
const MICROSECONDS_T I_DOWN[] PROGMEM = {9024U, 4512U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 39756};
const MICROSECONDS_T R_DOWN[] PROGMEM = {9024U, 2256U, 564U, 65535U};
const MICROSECONDS_T I_MUTE[] PROGMEM = { 9024U, 4512U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 564U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 39756 };
const MICROSECONDS_T R_MUTE[] PROGMEM = { 9024U, 2256U, 564U, 65535U };

void RGB_color(int redLightValue, int greenLightValue, int blueLightValue);
void RedLedOn();
void GreenLedOn();
void BlueLedOn();
void TurnLedOff();
void FlashRedLed();
void FlashGreenLed();
void FlashBlueLed();
void BlinkRedLed();
void BlinkGreenLed();
void BlinkBlueLed();
void Log(int msg);
void Log(String msg);

#endif