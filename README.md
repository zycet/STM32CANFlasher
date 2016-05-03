# STM32CANFlasher
A Tools for Flash the STM32 ARM Chip using CAN-Bus by C#

功能
* 支持读取 写入 擦除 芯片信息获取
* 支持Intel Hex格式程序二进制文件
* 支持Flash页描述配置
* 提供命令行参数程序
* 目前使用USB-CAN模块(https://item.taobao.com/item.htm?spm=a1z09.2.0.0.IBZCs1&id=39708440919&_u=322n3p905ca),已封装为接口,可替换.

运行环境
* .net Framework 3.5

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
* ST AN3154 <CAN Protocol Used in the STM32 Bootloader> V5
* ST AN2606 <Microcontroller System Memory Boot Mode> V20
