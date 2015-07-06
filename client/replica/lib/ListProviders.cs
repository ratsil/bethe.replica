using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using helpers.replica.services.dbinteract;
using helpers.extensions;

namespace replica.sl
{
    namespace ListProviders
    {
 		public class ListProvider<T> where T : new() //нужно отказываться от этого класса
		{
            static protected ObservableCollection<T> _Observable;
			static protected T[] _Array;
			static protected Queue<T> _Queue;
			static protected Dictionary<long, T> _Hash;

			static public T[] Array
            {
				get
				{
					return _Array;
				}
            }
			static public Queue<T> Queue
			{
				get
				{
					if (null == _Array)
						return null;
					if (null == _Queue)
						_Queue = new Queue<T>(_Array.Where(row => row != null));
					return _Queue;
				}
			}
			static public Dictionary<long, T> Hash
			{
				get
				{
					if (null == _Array)
						return null;
					if (null == _Hash)
					{
						_Hash = new Dictionary<long, T>();
						long nID;
						System.Reflection.PropertyInfo cPI = (System.Reflection.PropertyInfo)typeof(T).GetMember("nID")[0];
						foreach (T cT in _Array)
						{
							nID = (cPI).GetValue(cT, null).ToID();
							if (!_Hash.ContainsKey(nID))
								_Hash.Add(nID, cT);
						}
					}
					return _Hash;
				}
			}
			static public ObservableCollection<T> Observable
			{
				get
				{
					if (null == _Array)
						return null;
					if (null == _Observable)
						_Observable = new ObservableCollection<T>(_Array.Where(row => row != null));
					return _Observable;
				}
			}

			static public void Set(T[] ar)
			{
				_Array = ar;
				_Queue = null;
				_Hash = null;
                _Observable = null;
			}

			static public void Add(T[] ar)
			{
				if (null == _Array)
					Set(ar);
				else
					Set(_Array.Concat(ar).ToArray());
			}
			static public void Add(T newT)
			{
				if (null == newT)
					Add();
				else
					Add(new T[1]{newT});
			}
            static public void Add()
            {
                Observable.Add(new T());
                _Queue = null;
				_Hash = null;
				_Array = new T[Observable.Count];
				Observable.CopyTo(_Array, 0);
            }
            static public void Delete(T cT)
            {
                Observable.Remove(cT);
				_Queue = null;
				_Hash = null;
				_Array = new T[Observable.Count];
				Observable.CopyTo(_Array, 0);
			}
		}

        //public class Assets : ListProvider<Asset> { }
		public class Classes : ListProvider<Class> { }
        //public class Statuses : ListProvider<IdNamePair> { }
		public class PLIs : ListProvider<PlaylistItem> { }

        //public class Messages : ListProvider<Message> { }
        //public class Storages : ListProvider<Storage> { }
		public class Files : ListProvider<File> { }

        //public class Persons : ListProvider<Person> { }
        //public class Styles : ListProvider<IdNamePair> { }

		public class Rotations : ListProvider<IdNamePair> { }
		public class Palettes : ListProvider<IdNamePair> { }
		public class Sounds : ListProvider<IdNamePair> { }
	}
}
