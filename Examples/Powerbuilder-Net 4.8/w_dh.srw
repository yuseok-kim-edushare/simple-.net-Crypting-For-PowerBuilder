$PBExportHeader$w_dh.srw
forward
global type w_dh from window
end type
type st_8 from statictext within w_dh
end type
type st_7 from statictext within w_dh
end type
type sle_8 from singlelineedit within w_dh
end type
type sle_7 from singlelineedit within w_dh
end type
type sle_6 from singlelineedit within w_dh
end type
type sle_5 from singlelineedit within w_dh
end type
type sle_4 from singlelineedit within w_dh
end type
type sle_3 from singlelineedit within w_dh
end type
type sle_2 from singlelineedit within w_dh
end type
type sle_1 from singlelineedit within w_dh
end type
type st_6 from statictext within w_dh
end type
type st_5 from statictext within w_dh
end type
type st_3 from statictext within w_dh
end type
type st_4 from statictext within w_dh
end type
type st_2 from statictext within w_dh
end type
type st_1 from statictext within w_dh
end type
type cb_1 from commandbutton within w_dh
end type
end forward

global type w_dh from window
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
st_8 st_8
st_7 st_7
sle_8 sle_8
sle_7 sle_7
sle_6 sle_6
sle_5 sle_5
sle_4 sle_4
sle_3 sle_3
sle_2 sle_2
sle_1 sle_1
st_6 st_6
st_5 st_5
st_3 st_3
st_4 st_4
st_2 st_2
st_1 st_1
cb_1 cb_1
end type
global w_dh w_dh

on w_dh.create
this.st_8=create st_8
this.st_7=create st_7
this.sle_8=create sle_8
this.sle_7=create sle_7
this.sle_6=create sle_6
this.sle_5=create sle_5
this.sle_4=create sle_4
this.sle_3=create sle_3
this.sle_2=create sle_2
this.sle_1=create sle_1
this.st_6=create st_6
this.st_5=create st_5
this.st_3=create st_3
this.st_4=create st_4
this.st_2=create st_2
this.st_1=create st_1
this.cb_1=create cb_1
this.Control[]={this.st_8,&
this.st_7,&
this.sle_8,&
this.sle_7,&
this.sle_6,&
this.sle_5,&
this.sle_4,&
this.sle_3,&
this.sle_2,&
this.sle_1,&
this.st_6,&
this.st_5,&
this.st_3,&
this.st_4,&
this.st_2,&
this.st_1,&
this.cb_1}
end on

on w_dh.destroy
destroy(this.st_8)
destroy(this.st_7)
destroy(this.sle_8)
destroy(this.sle_7)
destroy(this.sle_6)
destroy(this.sle_5)
destroy(this.sle_4)
destroy(this.sle_3)
destroy(this.sle_2)
destroy(this.sle_1)
destroy(this.st_6)
destroy(this.st_5)
destroy(this.st_3)
destroy(this.st_4)
destroy(this.st_2)
destroy(this.st_1)
destroy(this.cb_1)
end on

type st_8 from statictext within w_dh
integer x = 78
integer y = 1708
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
string text = "Decrypt ~"test~""
boolean focusrectangle = false
end type

type st_7 from statictext within w_dh
integer x = 82
integer y = 1552
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
string text = "Encrypt ~"test~""
boolean focusrectangle = false
end type

type sle_8 from singlelineedit within w_dh
integer x = 622
integer y = 1688
integer width = 3259
integer height = 132
integer taborder = 20
integer textsize = -12
integer weight = 400
fontcharset fontcharset = ansi!
fontpitch fontpitch = variable!
fontfamily fontfamily = swiss!
string facename = "Tahoma"
long textcolor = 33554432
string text = "none"
borderstyle borderstyle = stylelowered!
end type

type sle_7 from singlelineedit within w_dh
integer x = 613
integer y = 1512
integer width = 3259
integer height = 132
integer taborder = 40
integer textsize = -12
integer weight = 400
fontcharset fontcharset = ansi!
fontpitch fontpitch = variable!
fontfamily fontfamily = swiss!
string facename = "Tahoma"
long textcolor = 33554432
string text = "none"
borderstyle borderstyle = stylelowered!
end type

type sle_6 from singlelineedit within w_dh
integer x = 805
integer y = 1068
integer width = 3259
integer height = 132
integer taborder = 30
integer textsize = -12
integer weight = 400
fontcharset fontcharset = ansi!
fontpitch fontpitch = variable!
fontfamily fontfamily = swiss!
string facename = "Tahoma"
long textcolor = 33554432
string text = "none"
borderstyle borderstyle = stylelowered!
end type

type sle_5 from singlelineedit within w_dh
integer x = 805
integer y = 896
integer width = 3259
integer height = 132
integer taborder = 30
integer textsize = -12
integer weight = 400
fontcharset fontcharset = ansi!
fontpitch fontpitch = variable!
fontfamily fontfamily = swiss!
string facename = "Tahoma"
long textcolor = 33554432
string text = "none"
borderstyle borderstyle = stylelowered!
end type

type sle_4 from singlelineedit within w_dh
integer x = 805
integer y = 732
integer width = 3259
integer height = 132
integer taborder = 20
integer textsize = -12
integer weight = 400
fontcharset fontcharset = ansi!
fontpitch fontpitch = variable!
fontfamily fontfamily = swiss!
string facename = "Tahoma"
long textcolor = 33554432
string text = "none"
borderstyle borderstyle = stylelowered!
end type

type sle_3 from singlelineedit within w_dh
integer x = 805
integer y = 560
integer width = 3259
integer height = 132
integer taborder = 20
integer textsize = -12
integer weight = 400
fontcharset fontcharset = ansi!
fontpitch fontpitch = variable!
fontfamily fontfamily = swiss!
string facename = "Tahoma"
long textcolor = 33554432
string text = "none"
borderstyle borderstyle = stylelowered!
end type

type sle_2 from singlelineedit within w_dh
integer x = 805
integer y = 400
integer width = 3259
integer height = 132
integer taborder = 10
integer textsize = -12
integer weight = 400
fontcharset fontcharset = ansi!
fontpitch fontpitch = variable!
fontfamily fontfamily = swiss!
string facename = "Tahoma"
long textcolor = 33554432
string text = "none"
borderstyle borderstyle = stylelowered!
end type

type sle_1 from singlelineedit within w_dh
integer x = 805
integer y = 244
integer width = 3259
integer height = 132
integer taborder = 10
integer textsize = -12
integer weight = 400
fontcharset fontcharset = ansi!
fontpitch fontpitch = variable!
fontfamily fontfamily = swiss!
string facename = "Tahoma"
long textcolor = 33554432
string text = "none"
borderstyle borderstyle = stylelowered!
end type

type st_6 from statictext within w_dh
integer x = 261
integer y = 1096
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
string text = "Shared 2"
boolean focusrectangle = false
end type

type st_5 from statictext within w_dh
integer x = 261
integer y = 924
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
string text = "Shared 1"
boolean focusrectangle = false
end type

type st_3 from statictext within w_dh
integer x = 261
integer y = 588
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
string text = "DH pub 2"
boolean focusrectangle = false
end type

type st_4 from statictext within w_dh
integer x = 261
integer y = 760
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
string text = "DH pri 2"
boolean focusrectangle = false
end type

type st_2 from statictext within w_dh
integer x = 261
integer y = 428
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
string text = "DH pri 1"
boolean focusrectangle = false
end type

type st_1 from statictext within w_dh
integer x = 261
integer y = 272
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
string text = "DH pub 1"
boolean focusrectangle = false
end type

type cb_1 from commandbutton within w_dh
integer x = 1152
integer y = 1316
integer width = 2098
integer height = 132
integer taborder = 10
integer textsize = -12
integer weight = 400
fontcharset fontcharset = ansi!
fontpitch fontpitch = variable!
fontfamily fontfamily = swiss!
string facename = "Tahoma"
string text = "exe"
end type

event clicked;nvo_encryptionhelper l_nvo_encryption_helper
l_nvo_encryption_helper = create nvo_encryptionhelper

string ls_key1[1 to 2]
string ls_key2[1 to 2]
string ls_share1, ls_share2

ls_key1 = l_nvo_encryption_helper.of_generatediffiehellmankeys( )
ls_key2 = l_nvo_encryption_helper.of_generatediffiehellmankeys( )

ls_share1 = l_nvo_encryption_helper.of_derivesharedkey( ls_key2[1], ls_key1[2])
ls_share2 = l_nvo_encryption_helper.of_derivesharedkey( ls_key1[1], ls_key2[2])

sle_1.text = ls_key1[1]
sle_2.text = ls_key1[2]
sle_3.text = ls_key2[1]
sle_4.text = ls_key2[2]
sle_5.text = ls_share1
sle_6.text = ls_share2

string ls_temp[1 to 2]
ls_temp = l_nvo_encryption_helper.of_encryptaescbcwithiv( 'test' , ls_share1)
sle_7.text = ls_temp[1]
sle_8.text = l_nvo_encryption_helper.of_decryptaescbcwithiv( ls_temp[1], ls_share2, ls_temp[2])
end event

