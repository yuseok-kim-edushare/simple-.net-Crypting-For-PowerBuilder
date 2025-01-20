forward
global type w_bcrypt from window
end type
type cb_1 from commandbutton within w_bcrypt
end type
type st_2 from statictext within w_bcrypt
end type
type st_1 from statictext within w_bcrypt
end type
type sle_encrypted from singlelineedit within w_bcrypt
end type
type sle_raw from singlelineedit within w_bcrypt
end type
end forward

global type w_bcrypt from window
integer width = 4754
integer height = 1980
boolean titlebar = true
string title = "Untitled"
boolean controlmenu = true
boolean minbox = true
boolean maxbox = true
boolean resizable = true
long backcolor = 67108864
string icon = "AppIcon!"
boolean center = true
cb_1 cb_1
st_2 st_2
st_1 st_1
sle_encrypted sle_encrypted
sle_raw sle_raw
end type
global w_bcrypt w_bcrypt

on w_bcrypt.create
this.cb_1=create cb_1
this.st_2=create st_2
this.st_1=create st_1
this.sle_encrypted=create sle_encrypted
this.sle_raw=create sle_raw
this.Control[]={this.cb_1,&
this.st_2,&
this.st_1,&
this.sle_encrypted,&
this.sle_raw}
end on

on w_bcrypt.destroy
destroy(this.cb_1)
destroy(this.st_2)
destroy(this.st_1)
destroy(this.sle_encrypted)
destroy(this.sle_raw)
end on

type cb_1 from commandbutton within w_bcrypt
integer x = 773
integer y = 1416
integer width = 457
integer height = 132
integer taborder = 20
integer textsize = -12
integer weight = 400
fontcharset fontcharset = ansi!
fontpitch fontpitch = variable!
fontfamily fontfamily = swiss!
string facename = "Tahoma"
string text = "execute"
end type

event clicked;messagebox("debug", sle_raw.text)
nvo_encryptionhelper l_nvo_encryption_helper
l_nvo_encryption_helper = create nvo_encryptionhelper
sle_encrypted.text = l_nvo_encryption_helper.of_bcryptencoding( sle_raw.text )
messagebox("debug", sle_encrypted.text)
boolean lb_result
lb_result = l_nvo_encryption_helper.of_verifybcryptpassword(sle_raw.text, sle_encrypted.text)
if lb_result  then
	messagebox("debug", "Login Password Matched")
else
	messagebox("debug", "Wrong Password")
	sle_raw.text = ""
	sle_encrypted.text = ""
end if
end event

type st_2 from statictext within w_bcrypt
integer x = 421
integer y = 688
integer width = 457
integer height = 76
integer textsize = -12
integer weight = 400
fontcharset fontcharset = ansi!
fontpitch fontpitch = variable!
fontfamily fontfamily = swiss!
string facename = "Tahoma"
long textcolor = 33554432
long backcolor = 67108864
string text = "Encrypted"
boolean focusrectangle = false
end type

type st_1 from statictext within w_bcrypt
integer x = 421
integer y = 488
integer width = 457
integer height = 76
integer textsize = -12
integer weight = 400
fontcharset fontcharset = ansi!
fontpitch fontpitch = variable!
fontfamily fontfamily = swiss!
string facename = "Tahoma"
long textcolor = 33554432
long backcolor = 67108864
string text = "Raw"
boolean focusrectangle = false
end type

type sle_encrypted from singlelineedit within w_bcrypt
integer x = 942
integer y = 664
integer width = 2578
integer height = 132
integer textsize = -12
integer weight = 400
fontcharset fontcharset = ansi!
fontpitch fontpitch = variable!
fontfamily fontfamily = swiss!
string facename = "Tahoma"
long textcolor = 33554432
boolean displayonly = true
borderstyle borderstyle = stylelowered!
end type

type sle_raw from singlelineedit within w_bcrypt
integer x = 942
integer y = 492
integer width = 1193
integer height = 132
integer taborder = 10
integer textsize = -12
integer weight = 400
fontcharset fontcharset = ansi!
fontpitch fontpitch = variable!
fontfamily fontfamily = swiss!
string facename = "Tahoma"
long textcolor = 33554432
borderstyle borderstyle = stylelowered!
end type

