﻿using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;

namespace Fulu.Core.Extensions
{
    public static class ListExtensions
    {
        public static T Clone<T>(this T RealObject)
        {
            string jsonData = JsonConvert.SerializeObject(RealObject);
            return JsonConvert.DeserializeObject<T>(jsonData);
            //using (Stream objectStream = new MemoryStream())
            //{
            //    //利用 System.Runtime.Serialization序列化与反序列化完成引用对象的复制
            //    IFormatter formatter = new BinaryFormatter();
            //    formatter.Serialize(objectStream, RealObject);
            //    objectStream.Seek(0, SeekOrigin.Begin);
            //    return (T)formatter.Deserialize(objectStream);

            //}
        }
    }
}
