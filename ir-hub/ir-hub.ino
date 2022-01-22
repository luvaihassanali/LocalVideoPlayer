#include "PinDefinitionsAndMore.h"
#include <IRremote.h> 

#define IR_CODE_NUM_BITS 32

IRsend irsend;
IRrecv receiver(7); 
decode_results results; 
unsigned long key_value = 0; 
bool powerPressed = false;
bool inputSourceSwitch = false;

void setup() {
  Serial.begin(9600); 
  receiver.enableIRIn(); 
  receiver.blink13(true);
  //Serial.println("Ready to receive");
  IrSender.begin(IR_SEND_PIN, ENABLE_LED_FEEDBACK); 
  //Serial.println("Ready to send...");
}

void loop() {
  DecodeIrValue();
}

void DecodeIrValue() {
  if (receiver.decode(&results)) {
    if (results.value == 0XFFFFFFFF) {
      results.value = key_value;
    }
    //Serial.println(results.value, HEX);
    switch (results.decode_type) {
      //case NEC:
      case SAMSUNG:
        ParseIrValue(results.value);
        break;
      default:
      case UNKNOWN:
        //Serial.println("UNKNOWN");
        break ;
    }
    key_value = results.value;
    receiver.resume();
  }
}

void ParseIrValue(int value) {
  switch (value) {
    case 0XE0E0629D:
      //Serial.println("Stop button");
      Serial.println("stop");
      break;
    case 0XE0E0E21D:
      //Serial.println("Play button");
      break;
    case 0XE0E052AD:
      //Serial.println("Pause button");
      break;
    case 0XE0E0A25D:
      //Serial.println("Rewind button");
      break;
    case 0XE0E012ED:
      //Serial.println("Forward button");
      break;
    case 0XE0E036C9:
      //Serial.println("A button");
      if(powerPressed) {
        PowerOffSoundBar();
      } else {
        PowerOnSoundBar();
      }
      break;
    case 0XE0E028D7:
      //Serial.println("B button - vol up");
      irsend.sendNEC(0x807F8877U, IR_CODE_NUM_BITS);
      delay(250);
      break;
    case 0XE0E0A857:
      //Serial.println("C button - vol down");
      irsend.sendNEC(0x807F10EFU, IR_CODE_NUM_BITS);
      delay(250);
      break;
    case 0XE0E06897:
      //Serial.println("D button");
      ChangeInputSoundBar();
      delay(250);
      break;
  }
}

void ChangeInputSoundBar() {
  if(inputSourceSwitch) {
    //Serial.println("Optical");
    irsend.sendNEC(0x807F926DU, IR_CODE_NUM_BITS);
    inputSourceSwitch = false;
    return;
  } else {
    //Serial.println("Bluetooth");
    irsend.sendNEC(0x807F52ADU, IR_CODE_NUM_BITS);
    inputSourceSwitch = true;
  }
}

void PowerOnSoundBar() { 
   //Serial.println("Power on");
   irsend.sendNEC(0x807F08F7U, IR_CODE_NUM_BITS);
   powerPressed = true;
   delay(250);
}

void PowerOffSoundBar() {
  //Serial.println("Power off");
  irsend.sendNEC(0x807F08F7U, IR_CODE_NUM_BITS);
  for (int i = 0; i < 300; i++) {
    delay(10);
    irsend.sendNEC(0XFFFFFFFF, 0);
  }
  powerPressed = false;
  delay(250);
}
