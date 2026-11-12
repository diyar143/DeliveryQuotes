@echo off
echo Building delivery_fee.dll ...
gcc -shared -o delivery_fee.dll delivery_fee.c
echo Done.
