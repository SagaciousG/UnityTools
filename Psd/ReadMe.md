使用步骤：
1.检查PSD命名，同级节点不允许重名
2.对不同类型层级定义关键词
   ---通用
      --@ignore 忽略该层级，如果是文件夹则忽略该文件夹及以下所有节点
   ---图片层级
      --@name=xxx 为当前层级取别名
      --@empty 只取当前层级的大小位置信息
      --@ys=xxx 为当前层添加PrefabStub组件
   ---文件夹层级
      --@grid=0x3 为当前层级添加GridLayoutGroup组件并且Space=0x3
      --@ver=3 为当前层级添加VerticalLayoutGroup组件并且Space=3
      --@hor=3 为当前层级添加HorizontalLayoutGroup组件并且Space=3
      --@list=xxx 为当前层级添加UIList组件
   ---Text层级
      --@font=zy 设置Text的font=zy
   