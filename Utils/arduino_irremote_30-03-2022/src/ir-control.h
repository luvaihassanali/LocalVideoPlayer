#ifndef IR_CONTROL_H
#define IR_CONTROL_H

#include <utils.h>
#include "PinDefinitionsAndMore.h"
#include <IRremote.h>

void sendRaw(const MICROSECONDS_T intro[], size_t lengthIntro, const MICROSECONDS_T repeat[], size_t lengthRepeat, FREQUENCY_T frequency, unsigned times);
void PowerSoundBar();
void SoundBarControl();
void SoundBarInput();
void PowerTv();;
void TvSoundInput();
void TvControl();

#endif