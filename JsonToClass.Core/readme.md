#### JsonToClass

在JsonToEntity中我们是规避掉了json转class这一步的，这一步由vs的`选择性粘贴`解决了；

现在JsonToClass-Console是想要自己做掉这一步，采用了一个开源组件`JsonCSharpClassGenerator`，地址在这里:https://github.com/JsonCSharpClassGenerator/JsonCSharpClassGenerator

JsonToClass-Console想要将其封装到自己内部，外部使用简单的命令行调用即可。现在的问题是console是 .net core的项目，而开源组件是经典的 .net framework。

所以我建立了该JsonToClass项目将原有组件port到 .net core平台，然后JsonToClass-Console直接引用JsonToClass项目。