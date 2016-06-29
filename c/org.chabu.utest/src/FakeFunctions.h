/*
 * FakeFunctions.h
 *
 *  Created on: 29.06.2016
 *      Author: Frank
 */

#ifndef FAKEFUNCTIONS_H_
#define FAKEFUNCTIONS_H_

#include "fff.h"
#include "Chabu.h"

DECLARE_FAKE_VOID_FUNC5(errorFunction, void*, enum Chabu_ErrorCode, const char* , int , const char*  );
DECLARE_FAKE_VOID_FUNC1(configureChannels, void*  );
DECLARE_FAKE_VALUE_FUNC1(struct Buffer*, channelGetXmitBuffer, void*  );
DECLARE_FAKE_VOID_FUNC1(channelXmitCompleted, void*  );
DECLARE_FAKE_VALUE_FUNC1(struct Buffer*, channelGetRecvBuffer, void*  );
DECLARE_FAKE_VOID_FUNC1(channelRecvCompleted, void*  );
DECLARE_FAKE_VOID_FUNC1(networkRegisterWriteRequest, void*  );
DECLARE_FAKE_VALUE_FUNC2(int, networkRecvBuffer, void*, struct Buffer* );
DECLARE_FAKE_VALUE_FUNC2(int, networkXmitBuffer, void*, struct Buffer* );


void FakeFunctions_ResetAll();

#endif /* FAKEFUNCTIONS_H_ */
