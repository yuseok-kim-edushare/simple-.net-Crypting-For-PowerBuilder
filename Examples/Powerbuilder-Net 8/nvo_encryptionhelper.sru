$PBExportHeader$nvo_encryptionhelper.sru
forward
global type nvo_encryptionhelper from dotnetobject
end type
end forward

global type nvo_encryptionhelper from dotnetobject
event ue_error ( )
end type
global nvo_encryptionhelper nvo_encryptionhelper

type variables

PUBLIC:
String is_assemblypath = "NET8.dll"
String is_classname = "SecureLibrary.EncryptionHelper"

/* Exception handling -- Indicates how proxy handles .NET exceptions */
Boolean ib_CrashOnException = False

/*      Error types       */
Constant Int SUCCESS        =  0 // No error since latest reset
Constant Int LOAD_FAILURE   = -1 // Failed to load assembly
Constant Int CREATE_FAILURE = -2 // Failed to create .NET object
Constant Int CALL_FAILURE   = -3 // Call to .NET function failed

/* Latest error -- Public reset via of_ResetError */
PRIVATEWRITE Long il_ErrorType   
PRIVATEWRITE Long il_ErrorNumber 
PRIVATEWRITE String is_ErrorText 

PRIVATE:
/*  .NET object creation */
Boolean ib_objectCreated

/* Error handler -- Public access via of_SetErrorHandler/of_ResetErrorHandler/of_GetErrorHandler
    Triggers "ue_Error" event for each error when no current error handler */
PowerObject ipo_errorHandler // Each error triggers <ErrorHandler, ErrorEvent>
String is_errorEvent
end variables

forward prototypes
public subroutine of_seterrorhandler (powerobject apo_newhandler, string as_newevent)
public subroutine of_signalerror ()
private subroutine of_setdotneterror (string as_failedfunction, string as_errortext)
public subroutine of_reseterror ()
public function boolean of_createondemand ()
private subroutine of_setassemblyerror (long al_errortype, string as_actiontext, long al_errornumber, string as_errortext)
public subroutine of_geterrorhandler (ref powerobject apo_currenthandler,ref string as_currentevent)
public subroutine of_reseterrorhandler ()
public function string of_encryptaesgcm(string as_plaintext,string as_base64key)
public function string of_decryptaesgcm(string as_combineddata,string as_base64key)
public function any of_encryptaescbcwithiv(string as_plaintext,string as_base64key)
public function string of_decryptaescbcwithiv(string as_base64ciphertext,string as_base64key,string as_base64iv)
public function string of_keygenaes256()
public function any of_generatediffiehellmankeys()
public function string of_derivesharedkey(string as_otherpartypublickeybase64,string as_privatekeybase64)
public function string of_bcryptencoding(string as_password)
public function boolean of_verifybcryptpassword(string as_password,string as_hashedpassword)
end prototypes

event ue_error ( );
/*-----------------------------------------------------------------------------------------*/
/*  Handler undefined or call failed (event undefined) => Signal object itself */
/*-----------------------------------------------------------------------------------------*/
end event

public subroutine of_seterrorhandler (powerobject apo_newhandler, string as_newevent);
//*-----------------------------------------------------------------*/
//*    of_seterrorhandler:  
//*                       Register new error handler (incl. event)
//*-----------------------------------------------------------------*/

This.ipo_errorHandler = apo_newHandler
This.is_errorEvent = Trim(as_newEvent)
end subroutine

public subroutine of_signalerror ();
//*-----------------------------------------------------------------------------*/
//* PRIVATE of_SignalError
//* Triggers error event on previously defined error handler.
//* Calls object's own UE_ERROR when handler or its event is undefined.
//*
//* Handler is "DEFINED" when
//* 	1) <ErrorEvent> is non-empty
//*	2) <ErrorHandler> refers to valid object
//*	3) <ErrorEvent> is actual event on <ErrorHandler>
//*-----------------------------------------------------------------------------*/

Boolean lb_handlerDefined
If This.is_errorEvent > '' Then
	If Not IsNull(This.ipo_errorHandler) Then
		lb_handlerDefined = IsValid(This.ipo_errorHandler)
	End If
End If

If lb_handlerDefined Then
	/* Try to call defined handler*/
	Long ll_status
	ll_status = This.ipo_errorHandler.TriggerEvent(This.is_errorEvent)
	If ll_status = 1 Then Return
End If

/* Handler undefined or call failed (event undefined) => Signal object itself*/
This.event ue_Error( )
end subroutine

private subroutine of_setdotneterror (string as_failedfunction, string as_errortext);
//*----------------------------------------------------------------------------------------*/
//* PRIVATE of_setDotNETError
//* Sets error description for specified error condition exposed by call to .NET  
//*
//* Error description layout
//*			| Call <failedFunction> failed.<EOL>
//*			| Error Text: <errorText> (*)
//* (*): Line skipped when <ErrorText> is empty
//*----------------------------------------------------------------------------------------*/

/* Format description*/
String ls_error
ls_error = "Call " + as_failedFunction + " failed."
If Len(Trim(as_errorText)) > 0 Then
	ls_error += "~r~nError Text: " + as_errorText
End If

/* Retain state in instance variables*/
This.il_ErrorType = This.CALL_FAILURE
This.is_ErrorText = ls_error
This.il_ErrorNumber = 0
end subroutine

public subroutine of_reseterror ();
//*--------------------------------------------*/
//* PUBLIC of_ResetError
//* Clears previously registered error
//*--------------------------------------------*/

This.il_ErrorType = This.SUCCESS
This.is_ErrorText = ''
This.il_ErrorNumber = 0
end subroutine

public function boolean of_createondemand ();
//*--------------------------------------------------------------*/
//*  PUBLIC   of_createOnDemand( )
//*  Return   True:  .NET object created
//*               False: Failed to create .NET object
//*  Loads .NET assembly and creates instance of .NET class.
//*  Uses .NET when loading .NET assembly.
//*  Signals error If an error occurs.
//*  Resets any prior error when load + create succeeds.
//*--------------------------------------------------------------*/

This.of_ResetError( )
If This.ib_objectCreated Then Return True // Already created => DONE

Long ll_status 
String ls_action

/* Load assembly using .NET */
ls_action = 'Load ' + This.is_AssemblyPath
DotNetAssembly lnv_assembly
lnv_assembly = Create DotNetAssembly
ll_status = lnv_assembly.LoadWithDotNet(This.is_AssemblyPath)

/* Abort when load fails */
If ll_status <> 1 Then
	This.of_SetAssemblyError(This.LOAD_FAILURE, ls_action, ll_status, lnv_assembly.ErrorText)
	This.of_SignalError( )
	Return False // Load failed => ABORT
End If

/*   Create .NET object */
ls_action = 'Create ' + This.is_ClassName
ll_status = lnv_assembly.CreateInstance(is_ClassName, This)

/* Abort when create fails */
If ll_status <> 1 Then
	This.of_SetAssemblyError(This.CREATE_FAILURE, ls_action, ll_status, lnv_assembly.ErrorText)
	This.of_SignalError( )
	Return False // Load failed => ABORT
End If

This.ib_objectCreated = True
Return True
end function

private subroutine of_setassemblyerror (long al_errortype, string as_actiontext, long al_errornumber, string as_errortext);
//*----------------------------------------------------------------------------------------------*/
//* PRIVATE of_setAssemblyError
//* Sets error description for specified error condition report by an assembly function
//*
//* Error description layout
//* 		| <actionText> failed.<EOL>
//* 		| Error Number: <errorNumber><EOL>
//* 		| Error Text: <errorText> (*)
//*  (*): Line skipped when <ErrorText> is empty
//*----------------------------------------------------------------------------------------------*/

/*    Format description */
String ls_error
ls_error = as_actionText + " failed.~r~n"
ls_error += "Error Number: " + String(al_errorNumber) + "."
If Len(Trim(as_errorText)) > 0 Then
	ls_error += "~r~nError Text: " + as_errorText
End If

/*  Retain state in instance variables */
This.il_ErrorType = al_errorType
This.is_ErrorText = ls_error
This.il_ErrorNumber = al_errorNumber
end subroutine

public subroutine of_geterrorhandler (ref powerobject apo_currenthandler,ref string as_currentevent);
//*-------------------------------------------------------------------------*/
//* PUBLIC of_GetErrorHandler
//* Return as REF-parameters current error handler (incl. event)
//*-------------------------------------------------------------------------*/

apo_currentHandler = This.ipo_errorHandler
as_currentEvent = This.is_errorEvent
end subroutine

public subroutine of_reseterrorhandler ();
//*---------------------------------------------------*/
//* PUBLIC of_ResetErrorHandler
//* Removes current error handler (incl. event)
//*---------------------------------------------------*/

SetNull(This.ipo_errorHandler)
SetNull(This.is_errorEvent)
end subroutine

public function string of_encryptaesgcm(string as_plaintext,string as_base64key);
//*-----------------------------------------------------------------*/
//*  .NET function : EncryptAesGcm
//*   Argument:
//*              String as_plaintext
//*              String as_base64key
//*   Return : String
//*-----------------------------------------------------------------*/
/* .NET  function name */
String ls_function
String ls_result

/* Set the dotnet function name */
ls_function = "EncryptAesGcm"

Try
	/* Create .NET object */
	If Not This.of_createOnDemand( ) Then
		SetNull(ls_result)
		Return ls_result
	End If

	/* Trigger the dotnet function */
	ls_result = This.encryptaesgcm(as_plaintext,as_base64key)
	Return ls_result
Catch(runtimeerror re_error)

	If This.ib_CrashOnException Then Throw re_error

	/*   Handle .NET error */
	This.of_SetDotNETError(ls_function, re_error.text)
	This.of_SignalError( )

	/*  Indicate error occurred */
	SetNull(ls_result)
	Return ls_result
End Try
end function

public function string of_decryptaesgcm(string as_combineddata,string as_base64key);
//*-----------------------------------------------------------------*/
//*  .NET function : DecryptAesGcm
//*   Argument:
//*              String as_combineddata
//*              String as_base64key
//*   Return : String
//*-----------------------------------------------------------------*/
/* .NET  function name */
String ls_function
String ls_result

/* Set the dotnet function name */
ls_function = "DecryptAesGcm"

Try
	/* Create .NET object */
	If Not This.of_createOnDemand( ) Then
		SetNull(ls_result)
		Return ls_result
	End If

	/* Trigger the dotnet function */
	ls_result = This.decryptaesgcm(as_combineddata,as_base64key)
	Return ls_result
Catch(runtimeerror re_error)

	If This.ib_CrashOnException Then Throw re_error

	/*   Handle .NET error */
	This.of_SetDotNETError(ls_function, re_error.text)
	This.of_SignalError( )

	/*  Indicate error occurred */
	SetNull(ls_result)
	Return ls_result
End Try
end function

public function any of_encryptaescbcwithiv(string as_plaintext,string as_base64key);
//*-----------------------------------------------------------------*/
//*  .NET function : EncryptAesCbcWithIv
//*   Argument:
//*              String as_plaintext
//*              String as_base64key
//*   Return : String[]
//*-----------------------------------------------------------------*/
/* .NET  function name */
String ls_function
Any ls_result

/* Set the dotnet function name */
ls_function = "EncryptAesCbcWithIv"

Try
	/* Create .NET object */
	If Not This.of_createOnDemand( ) Then
		SetNull(ls_result)
		Return ls_result
	End If

	/* Trigger the dotnet function */
	ls_result = This.encryptaescbcwithiv(as_plaintext,as_base64key)
	Return ls_result
Catch(runtimeerror re_error)

	If This.ib_CrashOnException Then Throw re_error

	/*   Handle .NET error */
	This.of_SetDotNETError(ls_function, re_error.text)
	This.of_SignalError( )

	/*  Indicate error occurred */
	SetNull(ls_result)
	Return ls_result
End Try
end function

public function string of_decryptaescbcwithiv(string as_base64ciphertext,string as_base64key,string as_base64iv);
//*-----------------------------------------------------------------*/
//*  .NET function : DecryptAesCbcWithIv
//*   Argument:
//*              String as_base64ciphertext
//*              String as_base64key
//*              String as_base64iv
//*   Return : String
//*-----------------------------------------------------------------*/
/* .NET  function name */
String ls_function
String ls_result

/* Set the dotnet function name */
ls_function = "DecryptAesCbcWithIv"

Try
	/* Create .NET object */
	If Not This.of_createOnDemand( ) Then
		SetNull(ls_result)
		Return ls_result
	End If

	/* Trigger the dotnet function */
	ls_result = This.decryptaescbcwithiv(as_base64ciphertext,as_base64key,as_base64iv)
	Return ls_result
Catch(runtimeerror re_error)

	If This.ib_CrashOnException Then Throw re_error

	/*   Handle .NET error */
	This.of_SetDotNETError(ls_function, re_error.text)
	This.of_SignalError( )

	/*  Indicate error occurred */
	SetNull(ls_result)
	Return ls_result
End Try
end function

public function string of_keygenaes256();
//*-----------------------------------------------------------------*/
//*  .NET function : KeyGenAES256
//*   Return : String
//*-----------------------------------------------------------------*/
/* .NET  function name */
String ls_function
String ls_result

/* Set the dotnet function name */
ls_function = "KeyGenAES256"

Try
	/* Create .NET object */
	If Not This.of_createOnDemand( ) Then
		SetNull(ls_result)
		Return ls_result
	End If

	/* Trigger the dotnet function */
	ls_result = This.keygenaes256()
	Return ls_result
Catch(runtimeerror re_error)

	If This.ib_CrashOnException Then Throw re_error

	/*   Handle .NET error */
	This.of_SetDotNETError(ls_function, re_error.text)
	This.of_SignalError( )

	/*  Indicate error occurred */
	SetNull(ls_result)
	Return ls_result
End Try
end function

public function any of_generatediffiehellmankeys();
//*-----------------------------------------------------------------*/
//*  .NET function : GenerateDiffieHellmanKeys
//*   Return : String[]
//*-----------------------------------------------------------------*/
/* .NET  function name */
String ls_function
Any ls_result

/* Set the dotnet function name */
ls_function = "GenerateDiffieHellmanKeys"

Try
	/* Create .NET object */
	If Not This.of_createOnDemand( ) Then
		SetNull(ls_result)
		Return ls_result
	End If

	/* Trigger the dotnet function */
	ls_result = This.generatediffiehellmankeys()
	Return ls_result
Catch(runtimeerror re_error)

	If This.ib_CrashOnException Then Throw re_error

	/*   Handle .NET error */
	This.of_SetDotNETError(ls_function, re_error.text)
	This.of_SignalError( )

	/*  Indicate error occurred */
	SetNull(ls_result)
	Return ls_result
End Try
end function

public function string of_derivesharedkey(string as_otherpartypublickeybase64,string as_privatekeybase64);
//*-----------------------------------------------------------------*/
//*  .NET function : DeriveSharedKey
//*   Argument:
//*              String as_otherpartypublickeybase64
//*              String as_privatekeybase64
//*   Return : String
//*-----------------------------------------------------------------*/
/* .NET  function name */
String ls_function
String ls_result

/* Set the dotnet function name */
ls_function = "DeriveSharedKey"

Try
	/* Create .NET object */
	If Not This.of_createOnDemand( ) Then
		SetNull(ls_result)
		Return ls_result
	End If

	/* Trigger the dotnet function */
	ls_result = This.derivesharedkey(as_otherpartypublickeybase64,as_privatekeybase64)
	Return ls_result
Catch(runtimeerror re_error)

	If This.ib_CrashOnException Then Throw re_error

	/*   Handle .NET error */
	This.of_SetDotNETError(ls_function, re_error.text)
	This.of_SignalError( )

	/*  Indicate error occurred */
	SetNull(ls_result)
	Return ls_result
End Try
end function

public function string of_bcryptencoding(string as_password);
//*-----------------------------------------------------------------*/
//*  .NET function : BcryptEncoding
//*   Argument:
//*              String as_password
//*   Return : String
//*-----------------------------------------------------------------*/
/* .NET  function name */
String ls_function
String ls_result

/* Set the dotnet function name */
ls_function = "BcryptEncoding"

Try
	/* Create .NET object */
	If Not This.of_createOnDemand( ) Then
		SetNull(ls_result)
		Return ls_result
	End If

	/* Trigger the dotnet function */
	ls_result = This.bcryptencoding(as_password)
	Return ls_result
Catch(runtimeerror re_error)

	If This.ib_CrashOnException Then Throw re_error

	/*   Handle .NET error */
	This.of_SetDotNETError(ls_function, re_error.text)
	This.of_SignalError( )

	/*  Indicate error occurred */
	SetNull(ls_result)
	Return ls_result
End Try
end function

public function boolean of_verifybcryptpassword(string as_password,string as_hashedpassword);
//*-----------------------------------------------------------------*/
//*  .NET function : VerifyBcryptPassword
//*   Argument:
//*              String as_password
//*              String as_hashedpassword
//*   Return : Boolean
//*-----------------------------------------------------------------*/
/* .NET  function name */
String ls_function
Boolean lbln_result

/* Set the dotnet function name */
ls_function = "VerifyBcryptPassword"

Try
	/* Create .NET object */
	If Not This.of_createOnDemand( ) Then
		SetNull(lbln_result)
		Return lbln_result
	End If

	/* Trigger the dotnet function */
	lbln_result = This.verifybcryptpassword(as_password,as_hashedpassword)
	Return lbln_result
Catch(runtimeerror re_error)

	If This.ib_CrashOnException Then Throw re_error

	/*   Handle .NET error */
	This.of_SetDotNETError(ls_function, re_error.text)
	This.of_SignalError( )

	/*  Indicate error occurred */
	SetNull(lbln_result)
	Return lbln_result
End Try
end function

on nvo_encryptionhelper.create
call super::create
triggerevent( this, "constructor" )
end on

on nvo_encryptionhelper.destroy
triggerevent( this, "destructor" )
call super::destroy
end on

