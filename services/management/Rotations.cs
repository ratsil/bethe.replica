using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;

using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Collections;
using System.IO;
using System.Threading;
using helpers;
using helpers.replica.pl;
using helpers.replica.mam;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;
using System.Management;

namespace replica.management
{
	class Rotations
	{
		public enum Type
		{
			first = 1,
			second = 2,
			third = 3,
			foreign = 4
		}
		private class RArray
		{
			List<Clip> _aRotation, _aSecondary;
			int _ni, _nj, _CurrentIndex;
			DateTime _dtCurrentDate;
			Clip _cLastGotSecondary;
			bool _bIndexChanged;
			Type _eType;
			static Dictionary<Type, RArray> _aAllRotations = new Dictionary<Type, RArray>();
			public int Count
			{
				get
				{
					if (null != _aRotation)
						return _aRotation.Count;
					else
						return -1;
				}
			}
			public bool AllItemsWasGot
			{
				get
				{
					if (1 > Count || (_CurrentIndex == _ni && _bIndexChanged))
						return true;
					else
						return false;
				}
			}
			public RArray(Type eType)
			{
				_eType = eType;
				_aRotation = new List<Clip>();
				_aSecondary = new List<Clip>();
				_ni = 0;
				_nj = 0;
				_aAllRotations.Add(eType, this);
			}
			public void Add(Clip cClip)
			{
				if (null != cClip)
					_aRotation.Add(cClip);
			}
			public Clip GetNext(DateTime dtDate)
			{
				if (dtDate != _dtCurrentDate)
				{
					_CurrentIndex = _ni;
					_bIndexChanged = false;
					_dtCurrentDate = dtDate;
					if (null != _cLastGotSecondary && _aSecondary.Contains(_cLastGotSecondary))
					{
						_aSecondary.Remove(_cLastGotSecondary);
						(new Logger()).WriteDebug2("Services.cs: GetNext: removing from secondary queue ---------- [type=" + this._eType + "] [clip=" + _cLastGotSecondary.sName + "] [secondary.count=" + _aSecondary.Count + "]");
					}
					_nj = 0;
				}
				if (0 < _aSecondary.Count && _nj < _aSecondary.Count)
				{
					return _cLastGotSecondary = _aSecondary[_nj++];
				}
				else if (_nj == _aSecondary.Count)  // все уже перебраны и все заблокированы....
					_cLastGotSecondary = null;

				if (_aRotation.Count == _ni)
					_ni = 0;
				if (!AllItemsWasGot)
				{
					_bIndexChanged = true;
					return _aRotation[_ni++];
				}
				else
					return null;
			}
			static public void SetAside(Type eType, Clip cClip)
			{
				RArray cCurrent = _aAllRotations[eType];
				if (!cCurrent._aSecondary.Contains(cClip))
					cCurrent._aSecondary.Add(cClip);
				(new Logger()).WriteDebug2("Services.cs: SetAside: [type=" + eType + "] [clip=" + cClip.sName + "] [secondary.count=" + cCurrent._aSecondary.Count + "]");
			}
			static public void Clear()
			{
				_aAllRotations.Clear();
			}
		}
		DBInteract _cDBI;
		Dictionary<long, DateTime> _ahArtistsBlocked;
		Dictionary<long, DateTime> _ahForeignArtistsBlocked;
		Dictionary<long, DateTime> _ahClipBlocked;
		Dictionary<long, DateTime> _ahLastPlayedInArchive;
		RArray _aRotation_1, _aRotation_2, _aRotation_3, _aRotation_4;
		public Rotations(Queue<Clip> aqClips)
		{
			_cDBI = new DBInteract();
			_ahArtistsBlocked = new Dictionary<long, DateTime>();
			_ahForeignArtistsBlocked = new Dictionary<long, DateTime>();
			_ahClipBlocked = new Dictionary<long, DateTime>();
			_ahLastPlayedInArchive = _cDBI.ArchiveLastPlayedAssetsGet();
			RArray.Clear();
			RotationsSet(aqClips);
		}
		private void RotationsSet(Queue<Clip> aqClips)
		{
			Clip cClip;
			_aRotation_1 = new RArray(Type.first);
			_aRotation_2 = new RArray(Type.second);
			_aRotation_3 = new RArray(Type.third);
			_aRotation_4 = new RArray(Type.foreign);
			while (0 < aqClips.Count)
			{
				cClip = aqClips.Dequeue();
				if (null != _ahLastPlayedInArchive && cClip.dtLastPlayed == DateTime.MaxValue && _ahLastPlayedInArchive.ContainsKey(cClip.nID))
					cClip.dtLastPlayed = _ahLastPlayedInArchive[cClip.nID];
				cClip.PersonsLoad();
				BlocksSet(cClip, cClip.dtLastPlayed);
				if (1 > cClip.nFramesQty)
				{
					try
					{
						cClip.nFramesQty = (int)_cDBI.AssetDurationUpdate((Asset)cClip);
					}
					catch (Exception ex)
					{
						(new Logger()).WriteError(ex);
						continue;
					}
				}
				switch (ConvertRotationNameToType(cClip.cRotation.sName))
				{
					case Type.first:
						_aRotation_1.Add(cClip);
						break;
					case Type.second:
						_aRotation_2.Add(cClip);
						break;
					case Type.third:
						_aRotation_3.Add(cClip);
						break;
					case Type.foreign:
						_aRotation_4.Add(cClip);
						break;
				}
			}
		}
		private Type ConvertRotationNameToType(string sRotationName)
		{
			switch (sRotationName)
			{
				case "частая":
					return Type.first;
				case "средняя":
					return Type.second;
				case "редкая":
					return Type.third;
				case "иностранная":
					return Type.foreign;
			}
			return Type.third;
		}
		public Clip GetNextClip(Type eType, DateTime dtTargetTime)
		{
			Type eCurType = GetTypeIfRotationIsEmpty(eType);
			Clip cRetVal;
			while (true)
			{
				switch (eCurType)
				{
					case Type.first:
						cRetVal = _aRotation_1.GetNext(dtTargetTime);
						break;
					case Type.second:
						cRetVal = _aRotation_2.GetNext(dtTargetTime);
						break;
					case Type.foreign:
						cRetVal = _aRotation_4.GetNext(dtTargetTime);
						break;
					case Type.third:
					default:
						cRetVal = _aRotation_3.GetNext(dtTargetTime);
						break;
				}
				if (!IsThisBlocked(cRetVal, dtTargetTime))
					break;
				RArray.SetAside(eCurType, cRetVal);
				eCurType = GetTypeIfRotationIsEmpty(eType);
			}
			BlocksSet(cRetVal, dtTargetTime);
			return cRetVal;
		}
		private Type GetTypeIfRotationIsEmpty(Type eType)
		{
			if (eType == Type.first && (_aRotation_1.AllItemsWasGot)) // т.е. или пусто или по кругу уже обошли....
				eType = Type.second;
			if (eType == Type.second && (_aRotation_2.AllItemsWasGot))
				eType = Type.third;
			if (eType == Type.foreign && (_aRotation_4.AllItemsWasGot))
				eType = Type.third;
			if (eType == Type.third && (_aRotation_3.AllItemsWasGot))
				throw new Exception("Кончились, либо блокированы клипы в третьей ротации, чего быть не должно...");
			return eType;
		}
		private void BlocksSet(Clip cCli, DateTime dtTime)
		{
			DateTime dtClipBlock = DateTime.MinValue, dtArtistBlock = DateTime.MinValue, dtForeignArtistBlock = DateTime.MinValue;
			if (DateTime.MaxValue > dtTime)
			{
				dtClipBlock = dtTime.Add(Preferences.tsBlockClipDuration);
				dtArtistBlock = dtTime.Add(Preferences.tsBlockArtistDuration);
				dtForeignArtistBlock = dtTime.Add(Preferences.tsBlockForeignDuration);
			}
			if (!_ahClipBlocked.ContainsKey(cCli.nID))
				_ahClipBlocked.Add(cCli.nID, dtClipBlock);
			else
				_ahClipBlocked[cCli.nID] = dtClipBlock;
			foreach (Person cPers in cCli.aPersons)
			{
				if (Type.foreign == ConvertRotationNameToType(cCli.cRotation.sName))
				{
					if (!_ahForeignArtistsBlocked.ContainsKey(cPers.nID))
						_ahForeignArtistsBlocked.Add(cPers.nID, dtForeignArtistBlock);
					else
						_ahForeignArtistsBlocked[cPers.nID] = dtForeignArtistBlock;
				}
				else
				{
					if (!_ahArtistsBlocked.ContainsKey(cPers.nID))
						_ahArtistsBlocked.Add(cPers.nID, dtArtistBlock);
					else
						_ahArtistsBlocked[cPers.nID] = dtArtistBlock;
				}
			}
		}
		private bool IsThisBlocked(Clip cCli, DateTime dtTargetTime)
		{
			if (_ahClipBlocked.ContainsKey(cCli.nID) && _ahClipBlocked[cCli.nID] > dtTargetTime)
				return true;
			foreach (Person cPers in cCli.aPersons)
			{
				if (Type.foreign == ConvertRotationNameToType(cCli.cRotation.sName))
				{
					if (_ahForeignArtistsBlocked.ContainsKey(cPers.nID) && _ahForeignArtistsBlocked[cPers.nID] > dtTargetTime)
						return true;
				}
				else
				{
					if (_ahArtistsBlocked.ContainsKey(cPers.nID) && _ahArtistsBlocked[cPers.nID] > dtTargetTime)
						return true;
				}
			}
			return false;
		}
		public Type CurrentRotationNumberGet(int nTotalIndex)
		{
			int nClearIndex = nTotalIndex - nTotalIndex / 4; // без рекламы
			int nForeign = nClearIndex % 6;
			switch (nClearIndex % 4)
			{
				case 0:
				case 2:
					if (0 == nForeign)
						return Type.foreign;
					else
						return Type.third;
				case 1:
					return Type.first;
				case 3:
					return Type.second;
			}
			return Type.third;
		}
	}
}
