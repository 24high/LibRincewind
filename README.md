# LibRincewind

A bruteforce protected aproach for encryption using plain text ASCII passwords.<br>
<br>
What is it about?<br>
<br>
Normal encryption of plain text passwords can theoretically be cracked because decryption attempts with a failure password will result in non-ASCII data.<br>
<br>
<br>
LibRincewind combines any symmetrical algorithm with a rotational algorithm so that false tries can't be distinguished from valid ones.<br>
The trick is how the ASCII code is being rotaded.<br>
<br>
Updates<br>
17.04.2025<br>
-The encryption of the rotaded data has been removed<br>
-Updated the demo<br><br>
WIP<br>
A password manager using LibRincewind is currently being developed.<br><br>
How does it work?<br>
<br>
1.) The text is rotated using a random key for each character until it is valid ASCII<br>
2.) The key gets encrypted with a password using a base algorithm.<br>
<br>
Caveats:<br>
<br>
-The length of the plain text can be guessed, because it equals the length of the encryption/decryption key<br>
-The algorithm is still prone to wordlist attacks<br>
-The Rotation is using normal bitshifts, no circular shifting. thus there is some statistical imablance, which could make it possible to guess which symbols could be correct.
It also limits the key to 6 symbols per byte.
<br><br>
so when encrypting an 8 letter password, it will result in<br>
6^8 = 1.679.616 <br>
false positives which can't be distinguished from the real password, while requiring the same computation power for an attack.<br>



<br>
Usage:<br>
<br>
Encryption of passwords using a main password (password managers):<br>
CRincewind rw=new CRincewind("pluginlibrary.dll", 512);<br>
String enc=rw.encryptString("data","password1","password2");<br>
String dec=rw.decryptString(enc,"password1","password2");<br><br>
Creating custom plugins:<br>
<br>
Implement the interface found in LibRincewindPlugin.<br>
<br>
ToDo:<br>
IV's for each character<br><br>
Update v1.1:<br>
-Added demo sourcecode<br>
-Added password authentication
<br><br>
Contact E-Mail: decipher2k20@gmail.com
