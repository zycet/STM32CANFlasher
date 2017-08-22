# STM32CANFlasher
A Tools for Flash the STM32 ARM Chip using CAN-Bus by C#

功能
* 支持读取 写入 擦除 芯片信息获取
* 支持Intel Hex格式程序二进制文件
* 支持Flash页描述配置
* 提供命令行参数程序
* 目前使用USB-CAN模块(https://item.taobao.com/item.htm?spm=a1z09.2.0.0.IBZCs1&id=39708440919&_u=322n3p905ca)
* CAN驱动封装为接口,可替换

支持操作
* ACK
* Read
* Write
* Erase
* GetState
* Go

测试环境
* .Net Framework 3.5
* STM32F4DISCOVERY Kit(MB997B)
* STM32F40_41xxx-LED_Demo.hex(此程序会让开发板上PD12 PD13引脚连接的LED3 LED4闪烁)
* 连接CAN2接口(PB5 PB13)至CAN收发器并与USB-CAN模块连接.
* 将BOOT0引脚拉高
* 执行STM32CANFlasher.exe -HexFile="STM32F40_41xxx-LED_Demo.hex" -EraseOpt=OnlyUsed -FlashSectionFile="stm32f4xx_1mb.xml"

命令行参数
* -HexFile="待烧写的hex格式文件,必须设定"
* -EraseOpt=None(不擦除)/OnlyUsed(仅仅擦除hex文件使用的部分,需要配合FlashSection文件.)/All(全片擦除),默认All.
* -FlashSectionFile="Flash页描述文件"(C# XML串行化的JobMaker.FlashSectionStruct[]格式文件,EraseOpt为OnlyUsed时需设定)
* -ShowReceiveSend=yes/y/no/n(是否显示收发的CAN数据包,默认no)
* -CANDevFile="第三方CAN模块驱动接口.Net DLL文件名"(需要与可执行程序在同一目录,默认:ECANDev.dll)
* -CANDevClass="DLL中继承了BUAA.Device.ICAN接口的类的全名"(默认:BUAA.Device.ECANDev)
* -Timeout=操作超时时间(单位s,默认2s)
* -TimeoutErase=擦除操作超时时间(单位s,默认20s)
* -JumpToFlash=yes/y/no/n(是否在完成烧写后跳转至Flash程序执行起始位置,默认跳转位置为0x8000000,默认no)
* -CANDeviceNo=CAN设备号(默认0)
* -CANPortNo=CAN设备端口号(默认0)
* -CANBPS=CAN波特率(默认125000,ST官方要求)
* -CANSendOnce=yes/y/no/n(是否单次发送CAN数据包,默认no)

支持芯片
* STM32F105xx
* STM32F107xx
* STM32F405xx
* STM32F407xx
* STM32F415xx
* STM32F417xx
* STM32F427xx
* STM32F429xx
* STM32F437xx
* STM32F439xx

参考信息
* ST AN3154 "CAN Protocol Used in the STM32 Bootloader" V5
* ST AN2606 "Microcontroller System Memory Boot Mode" V20
