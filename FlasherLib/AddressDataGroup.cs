using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BUAA.Structure
{
    /// <summary>
    /// 带有基地址标识的数据组
    /// </summary>
    /// <typeparam name="T">数据元类型</typeparam>
    public class AddressDataGroup<T>
    {
        public List<Data<T>> Groups = new List<Data<T>>();

        public class Data<T>
        {
            public int Address;

            public List<T> Datas = new List<T>();

            public Data(int Address)
            {
                this.Address = Address;
            }

            public void Add(T Data)
            {
                Datas.Add(Data);
            }

            public void Add(T[] Data)
            {
                Datas.AddRange(Data);
            }

            public T[] GetArray()
            {
                return Datas.ToArray<T>();
            }
        }

        public bool IsOnlyAllowInsertEnd = true;

        public void Insert(int Address, T[] Datas)
        {
            bool rInsert = false;
            int startNo = 0;

            if (IsOnlyAllowInsertEnd)
                startNo = Groups.Count - 1;
            if (startNo < 0)
                startNo = 0;

            for (int i = startNo; i < Groups.Count; i++)
            {
                if (Groups[i].Address + Groups[i].Datas.Count == Address)
                {
                    Groups[i].Add(Datas);
                    rInsert = true;
                    break;
                }
            }

            if (rInsert == false)
            {
                Data<T> data = new Data<T>(Address);
                data.Add(Datas);
                Groups.Add(data);
            }
        }
    }
}
