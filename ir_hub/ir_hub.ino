#include <Arduino.h>
#include "PinDefinitionsAndMore.h"
#define DECODE_NEC // Includes Apple and Onkyo (Can't compile for Uno without)
#define DECODE_SAMSUNG
#define MARK_EXCESS_MICROS 10 // Adapt it to your IR receiver module. See also IRremote.h.
//#define NO_LED_FEEDBACK_CODE // halves ISR duration
//#define DEBUG // Activate this for lots of lovely debug output from the decoders.
//#define INFO // To see valuable informations from universal decoder for pulse width or pulse distance protocols
#include <IRremote.hpp> // Needs to be after define statements

uint16_t sAddress = 0xFFF1;
uint8_t sCommand = 0x76;
bool powerPressed = false;
bool inputSourceSwitch = false;

void setup() {
  Serial.begin(9600);

  IrReceiver.begin(IR_RECEIVE_PIN);
  IrSender.begin(IR_SEND_PIN, ENABLE_LED_FEEDBACK); // Specify send pin and enable feedback LED at default feedback LED pin
  //Serial.print(F("Ready to receive IR signals of protocols: "));
  //printActiveIRProtocols(&//Serial);
  //Serial.print(F("at pin "));
  //Serial.println(IR_RECEIVE_PIN);
  //Serial.print(F("Ready to send IR signals at pin "));
  //Serial.println(IR_SEND_PIN);
  IrSender.enableIROut(38); // Call it with 38 kHz to initialize the values printed below (see UnitTest example)
}

void loop() {
  checkReceive(sAddress & 0xFF, sCommand);
  delay(100);
}

void checkReceive(uint16_t aSentAddress, uint16_t aSentCommand) {
  // wait until signal has received
  delay((RECORD_GAP_MICROS / 1000) + 1);
  if (IrReceiver.decode()) {
    /*IrReceiver.printIRResultShort(&Serial);
    if (IrReceiver.decodedIRData.flags & IRDATA_FLAGS_WAS_OVERFLOW) {
      IrReceiver.decodedIRData.flags = false; // yes we have recognized the flag :-)
      Serial.println(F("Overflow detected"));
      Serial.println(F("Try to increase the \"RAW_BUFFER_LENGTH\" value of " STR(RAW_BUFFER_LENGTH) " in " __FILE__));
    } else {
      if (IrReceiver.decodedIRData.address != aSentAddress) {
        Serial.print(F("ERROR: Received address=0x"));
        Serial.print(IrReceiver.decodedIRData.address, HEX);
        Serial.print(F(" != sent address=0x"));
        Serial.println(aSentAddress, HEX);
      }

      if (IrReceiver.decodedIRData.command != aSentCommand) {
        Serial.print(F("ERROR: Received command=0x"));
        Serial.print(IrReceiver.decodedIRData.command, HEX);
        Serial.print(F(" != sent command=0x"));
        Serial.println(aSentCommand, HEX);
      }
    }*/
    uint32_t rawData = IrReceiver.decodedIRData.decodedRawData;
    ParseIrValue(rawData);
    IrReceiver.resume();
  }
}

void ParseIrValue(uint32_t rawData) {
  //Serial.print("ParseIrValue: ");
  //Serial.println(rawData, HEX);
  switch (rawData) {
    case 0x936C0707:
      //Serial.println("A");
      if (powerPressed) {
        PowerOffSoundBar();
      } else {
        PowerOnSoundBar();
      }
      break;
    case 0xEB140707:
      //Serial.println("B");
      IrSender.sendNECMSB(0x807F8877U, 32);
      delay(250);
      break;
    case 0xEA150707:
      //Serial.println("C");
      IrSender.sendNECMSB(0x807F10EFU, 32);
      break;
    case 0xE9160707:
      //Serial.println("D");
      ChangeInputSoundBar();
      break;
    case 0xB9460707:
      Serial.println("stop");
      Serial.flush();
      break;
  }
  delay(250);
}

void ChangeInputSoundBar() {
  if (inputSourceSwitch) {
    //Serial.println("Optical");
    IrSender.sendNECMSB(0x807F926DU, 32);
    inputSourceSwitch = false;
    return;
  } else {
    //Serial.println("Bluetooth");
    IrSender.sendNECMSB(0x807F52ADU, 32);
    inputSourceSwitch = true;
  }
}

void PowerOnSoundBar() {
  //Serial.println("Power on");
  IrSender.sendNECMSB(0x807F08F7U, 32);
  powerPressed = true;
}

void PowerOffSoundBar() {
  //Serial.println("Power off");
  IrSender.sendNECMSB(0x807F08F7U, 32);
  for (int i = 0; i < 150; i++) {
    delay(10);
    IrSender.sendNECMSB(0XFFFFFFFF, 0);
  }
  powerPressed = false;
}
