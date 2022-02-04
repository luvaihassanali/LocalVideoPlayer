#include <Arduino.h>
#include "PinDefinitionsAndMore.h"
#define DECODE_NEC
#define DECODE_SAMSUNG
#define MARK_EXCESS_MICROS 10
#define DEBUG false

#include <IRremote.hpp>

uint16_t sAddress = 0xFFF1;
uint8_t sCommand = 0x76;
typedef uint16_t microseconds_t; 
typedef uint16_t frequency_t;   

static inline unsigned hz2khz(frequency_t f) { return f / 1000U; }

const microseconds_t intro_Power[] PROGMEM = { 9024U, 4512U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 39756 };
const microseconds_t repeat_Power[] PROGMEM = { 9024U, 2256U, 564U, 65535U };
const microseconds_t intro_BT[] PROGMEM = { 9024U, 4512U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 564U, 564U, 1692U, 564U, 564U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 564U, 564U, 1692U, 564U, 564U, 564U, 1692U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 564U, 564U, 1692U, 564U, 39756 };
const microseconds_t repeat_BT[] PROGMEM = { 9024U, 2256U, 564U, 65535U };
const microseconds_t intro_Optical[] PROGMEM = { 9024U, 4512U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 564U, 564U, 1692U, 564U, 39756 };
const microseconds_t repeat_Optical[] PROGMEM = { 9024U, 2256U, 564U, 65535U };
const microseconds_t intro_up_arrow[] PROGMEM = { 9024U, 4512U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 39756 };
const microseconds_t repeat_up_arrow[] PROGMEM = { 9024U, 2256U, 564U, 65535U };
const microseconds_t intro_down_arrow[] PROGMEM = { 9024U, 4512U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 564U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 1692U, 564U, 39756 };
const microseconds_t repeat_down_arrow[] PROGMEM = { 9024U, 2256U, 564U, 65535U };

bool powerPressed = false;
bool inputSourceSwitch = false;

void setup() {
  Serial.begin(9600);
  IrReceiver.begin(IR_RECEIVE_PIN, DISABLE_LED_FEEDBACK);
  IrSender.begin(IR_SEND_PIN, ENABLE_LED_FEEDBACK);
}

void loop() {
  checkReceive(sAddress & 0xFF, sCommand);
  delay(100);
}

void checkReceive(uint16_t aSentAddress, uint16_t aSentCommand) {
  // wait until signal has received
  delay((RECORD_GAP_MICROS / 1000) + 1);
  if (IrReceiver.decode()) {
    if (DEBUG) { IrReceiver.printIRResultShort(&Serial); }
    uint32_t rawData = IrReceiver.decodedIRData.decodedRawData;
    ParseIrValue(rawData);
    IrReceiver.resume();
  }
}

void ParseIrValue(uint32_t rawData) {
  switch (rawData) {
    case 0x19E60707:
      if (DEBUG) { Serial.println("Samsung tv power signal intercepted"); }
      PowerSoundBarLong();
      break;
    case 0x936C0707:
      if (DEBUG) { Serial.println("A"); }
      PowerSoundBar();
      break;
    case 0xEB140707:
      if (DEBUG) { Serial.println("B"); }
      sendRaw(intro_up_arrow, 68U, repeat_up_arrow, 4U, 38400U, 1);
      delay(250);
      break;
    case 0xEA150707:
      if (DEBUG) { Serial.println("C"); }
      sendRaw(intro_down_arrow, 68U, repeat_down_arrow, 4U, 38400U, 1);
      break;
    case 0xE9160707:
      if (DEBUG) { Serial.println("D"); }
      ChangeInputSoundBar();
      break;
    case 0xB9460707:
      Serial.println("stop");
      Serial.flush();
      break;
  }
  delay(250);
}

static void sendRaw(const microseconds_t intro[], size_t lengthIntro, const microseconds_t repeat[], size_t lengthRepeat, frequency_t frequency, unsigned times) {
  if (lengthIntro > 0U) {
    IrSender.sendRaw_P(intro, lengthIntro, hz2khz(frequency));
  }
  if (lengthRepeat > 0U) {
    for (unsigned i = 0U; i < times - (lengthIntro > 0U); i++) {
      IrSender.sendRaw_P(repeat, lengthRepeat, hz2khz(frequency));
    }
  }
}

void ChangeInputSoundBar() {
  if (inputSourceSwitch) {
    if (DEBUG) { Serial.println("Optical"); }
    sendRaw(intro_BT, 68U, repeat_BT, 4U, 38400U, 1);
    inputSourceSwitch = false;
    return;
  } else {
    if (DEBUG) { Serial.println("Bluetooth"); }
    sendRaw(intro_Optical, 68U, repeat_Optical, 4U, 38400U, 1);
    inputSourceSwitch = true;
  }
}

void PowerSoundBar() {
  if (powerPressed) {
    sendRaw(intro_Power, 68U, repeat_Power, 4U, 38400U, 12);
    powerPressed = false;
    return;
  } else {
    sendRaw(intro_Power, 68U, repeat_Power, 4U, 38400U, 1);
    powerPressed = true;
  }
}

void PowerSoundBarLong() {
  if (powerPressed) {
    if (DEBUG) { Serial.println("Power off"); }
    sendRaw(intro_Power, 68U, repeat_Power, 4U, 38400U, 12);
    delay(100);
    sendRaw(intro_Power, 68U, repeat_Power, 4U, 38400U, 12);
    delay(100);
    sendRaw(intro_Power, 68U, repeat_Power, 4U, 38400U, 12);
    powerPressed = false;
    return;
  } else {
    if (DEBUG) { Serial.println("Power on"); }
    sendRaw(intro_Power, 68U, repeat_Power, 4U, 38400U, 1);
    delay(100);
    sendRaw(intro_Power, 68U, repeat_Power, 4U, 38400U, 1);
    powerPressed = true;
  }
}
