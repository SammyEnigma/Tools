#### 使用场景
- 记录日志时，通常会在message中拼接各种关心的参数的值
- 记录日志时，往往想要直接输出某个关心的对象的值

#### message拼接
```csharp
var _current_count = 1;
var _check_count = 10;
var _flush_interval = 20;
var r_count = CalcRowCount();

Log("开始flush处理", new { rows_count = r_count, _current_count, _check_count, _flush_interval });

// 输出如下：
// 2019/08/27 11:12:03.938|DEBUG|开始flush处理，参数：【rows_count=1，_current_count=1，_check_count=10，_flush_interval=20】 |ConsoleApp1.Test|
```

#### dump object
```csharp
class Apple
{
    public string value;
    public bool isOK; 
}

class Shop
{
    public int count;
    public string name;
    public List<Apple> data;
    public Apple single;
}

var list = new List<Shop>
    {
        new Shop { name="a", count=1, single=new Apple{ isOK=false, value="s_val1" }, data=new List<Apple> { new Apple { isOK=false, value="a_app_1" } } },
        new Shop { name="b", count=2, single=new Apple{ isOK=true, value="s_val2" }, data=new List<Apple> { new Apple { isOK=false, value="b_app_1" }, new Apple { isOK = false, value = "b_app_2" } } },
        new Shop { name="c", count=1, data=new List<Apple> { new Apple { isOK=true, value="c_app_1" } } }
    }

DumpObj(
    list, 
    skip: 0, // 跳过前n个
    take: 10, // 连续取mge
    p => p.name, // 类型的字段可能很多，我们可能只关心其中某些字段，通过
    p => p.single, // lambda来告诉方法输出的json中保留哪些字段的信息
    p => p.data);
```

#### 注意
工程代码中日志组件使用的nlog，因为只有一个cs文件而已，如果你喜欢其它的日志组件自行改改代码即可