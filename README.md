
###说明： 
    该工具旨在减少psd的学习负担，保留极少数psd关键词来创建组件并赋值，
    绝大数组件都在UGUI上手动添加。
###注意：
    该工具与之前Psd2UGUI不能混用，两者制作流程不一样。


###使用步骤：
1. 检查PSD命名，同级节点不允许重名  
2. 对不同类型层级定义关键词  

    |层级|关键词|说明|
    |-------|:-----|:---|
    |通用|@ignore|忽略该层级，如果是文件夹则忽略该文件夹及以下所有节点|
    |图片层级|@name=xxx|为当前层级取别名|
    |  |@empty|只取当前层级的大小位置信息|
    |  |@ys=xxx|为当前层添加PrefabStub组件，并赋值预设名|
    |文件夹层级|@grid=0x3|为当前层级添加GridLayoutGroup组件并且Space=0x3|
    |  |@ver=3|为当前层级添加VerticalLayoutGroup组件并且Space=3|
    |  |@hor=3|为当前层级添加HorizontalLayoutGroup组件并且Space=3|
    |  |@list=xxx|为当前层级添加UIList组件|
    |Text层级|@font=zy|设置Text的font=zy|
###流程：
1. 检查PSD命名，调整层级以匹配自己即将要使用的组件，如：制作list 
    - 创建Viewport文件夹，将.mask图片放至子节点  
   ![1](https://user-images.githubusercontent.com/49907344/167817107-b1079ad6-8b89-4cf0-b32f-52a6bb62837e.png)

    - 合并Content节点下的图片，并添加@ignore
2. 使用Psd工具生成预制体，因为**Scene1**下第一个Canvas是**BloodRoot**节点，所以  
最好新建一个场景，在***空场景***里生成psd
![2](https://user-images.githubusercontent.com/49907344/167817183-7caeee05-e684-45d2-bfb3-551b648e991f.png)

3. 如果已存在目标预制体，则会保留**同一层级**下**名称相同**的对象的组件和数据。重新生成时
只会改变部分组件的属性。
    - 在`PSDLayerGroup.SetVariableValue`,`PSDLayerImage.SetVariableValue`  
`PSDLayerText.SetVariableValue`定义不同层级的可变属性。
4. 如果不存在目标预制体，则会生成一个与Psd层级相同的预制体。为预制体添加**LuaBehaviour**
点击**FillName**获取（创建）lua文件路径。
![3](https://user-images.githubusercontent.com/49907344/167817196-80cbfeb4-af86-43bd-b6a7-d3261649f299.png)

5. 为层级添加组件。将需要引用的层级拖入luaBehaviour选区添加引用

###写在最后
1. 再次生成预制体时会保留数据（前提是找得到对应关系）
   
