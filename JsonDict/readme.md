#### 优势
- 不用额外定义封装类型
- 不用dynamic

#### 劣势
额外的空间开销

#### 基本使用
```csharp
var json_str = "{\"data\":[{\"id\":\"1\",\"name\":\"abc\",\"age\":\"10\"}],\"database\":\"test\",\"mysqlType\":{\"id\":\"int(4)\",\"name\":\"varchar(100)\",\"age\":\"int(4)\"},\"table\":\"userinfo\",\"type\":1}";
var json_obj = JsonConvert.DeserializeObject<JsonDict>(json_str);

var type = json_obj.GetInt("type");
var db = json_obj.GetString("database");
var table = json_obj.GetString("table");
var data = json_obj.GetList<JsonDict>("data");
var mysqlType = json_obj.GetDict("mysqlType");

foreach (var key in mysqlType.Keys)
{
    Console.WriteLine($"key:{key}, val:{mysqlType[key]}");
}
```