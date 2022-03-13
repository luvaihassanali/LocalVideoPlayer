#include <Arduino.h>
#include "PinDefinitionsAndMore.h"
// Defines must appear before IRremote include
#define DECODE_NEC
#define DECODE_SAMSUNG
#define MARK_EXCESS_MICROS 10
#include <IRremote.hpp>

const uint32_t SAMSUNG_POWER = 0x19E60707;
const uint32_t SAMSUNG_A = 0x936C0707;
const uint32_t SAMSUNG_B = 0xEB140707;
const uint32_t SAMSUNG_C = 0xEA150707;
const uint32_t SAMSUNG_D = 0xE9160707;
const uint32_t SAMSUNG_STOP = 0xB9460707;

const bool DEBUG = false;

// Sent address and command placeholder for checkReceive
uint16_t sAddress = 0xFFF1;
uint8_t sCommand = 0x76;

bool opticalBluetoothSwitch = false;
bool powerPressed = false;

// The following variables are automatically generated using IrScrutinizer 2.3.0 and Bomaker Ondine 1 Soundbar.rmdu
// http://www.hifi-remote.com/forums/dload.php?action=file&file_id=25809

typedef uint16_t microseconds_t;
typedef uint16_t frequency_t;

static inline unsigned hz2khz(frequency_t f) {
  return f / 1000U;
}

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

void setup() {
  Serial.begin(9600);
  IrReceiver.begin(IR_RECEIVE_PIN, DISABLE_LED_FEEDBACK);
  IrSender.begin(IR_SEND_PIN, ENABLE_LED_FEEDBACK);
  if (DEBUG) {
    Serial.println("Ready");
  }
}

void loop() {
  checkReceive(sAddress & 0xFF, sCommand);
  delay(100);
}

// https://github.com/Arduino-IRremote/Arduino-IRremote/tree/master/examples/UnitTest
void checkReceive(uint16_t aSentAddress, uint16_t aSentCommand) {
  // Wait until signal has received
  delay((RECORD_GAP_MICROS / 1000) + 1);
  if (IrReceiver.decode()) {
    if (DEBUG) {
      IrReceiver.printIRResultShort(&Serial);
    }
    uint32_t rawData = IrReceiver.decodedIRData.decodedRawData;
    ParseIrValue(rawData);
    IrReceiver.resume();
  }
}

void ParseIrValue(uint32_t rawData) {
  switch (rawData) {
    case SAMSUNG_POWER:
      if (DEBUG) {
        Serial.println("Tv power signal intercepted");
      }
      PowerSoundBarLong();
      break;
    case SAMSUNG_A:
      if (DEBUG) {
        Serial.println("A");
      }
      PowerSoundBar();
      break;
    case SAMSUNG_B:
      if (DEBUG) {
        Serial.println("B");
      }
      sendRaw(intro_up_arrow, 68U, repeat_up_arrow, 4U, 38400U, 1);
      break;
    case SAMSUNG_C:
      if (DEBUG) {
        Serial.println("C");
      }
      sendRaw(intro_down_arrow, 68U, repeat_down_arrow, 4U, 38400U, 1);
      break;
    case SAMSUNG_D:
      if (DEBUG) {
        Serial.println("D");
      }
      ChangeInputSoundBar();
      break;
    case SAMSUNG_STOP:
      // Transfer string to serial USB connection
      Serial.println("stop");
      // Wait for the transmission of outgoing serial data to complete
      Serial.flush();
      Led13Blink();
      break;
  }
  delay(200);
}

// https://github.com/Arduino-IRremote/Arduino-IRremote/tree/master/examples/SendRawDemo
void sendRaw(const microseconds_t intro[], size_t lengthIntro, const microseconds_t repeat[], size_t lengthRepeat, frequency_t frequency, unsigned times) {
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
  if (opticalBluetoothSwitch) {
    if (DEBUG) {
      Serial.println("Optical");
    }
    sendRaw(intro_BT, 68U, repeat_BT, 4U, 38400U, 1);
    opticalBluetoothSwitch = false;
    return;
  } else {
    if (DEBUG) {
      Serial.println("Bluetooth");
    }
    sendRaw(intro_Optical, 68U, repeat_Optical, 4U, 38400U, 1);
    opticalBluetoothSwitch = true;
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
    if (DEBUG) {
      Serial.println("Power off");
    }
    sendRaw(intro_Power, 68U, repeat_Power, 4U, 38400U, 12);
    //delay(100);
    //sendRaw(intro_Power, 68U, repeat_Power, 4U, 38400U, 12);
    powerPressed = false;
    return;
  } else {
    if (DEBUG) {
      Serial.println("Power on");
    }
    sendRaw(intro_Power, 68U, repeat_Power, 4U, 38400U, 1);
    //delay(100);
    //sendRaw(intro_Power, 68U, repeat_Power, 4U, 38400U, 1);
    powerPressed = true;
  }
}

void Led13Blink() {
  digitalWrite(LED_BUILTIN, HIGH);
  delay(50);
  digitalWrite(LED_BUILTIN, LOW);
}
