using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace MysqlTest.Fakes;

public class FakeDataReader : DbDataReader
{
    private readonly List<Dictionary<string, object>> _rows;
    private int _currentIndex = -1;
    private bool _isOpen = true;

    public FakeDataReader(List<Dictionary<string, object>> rows)
    {
        _rows = rows ?? new List<Dictionary<string, object>>();
    }

    public override bool Read()
    {
        _currentIndex++;
        return _currentIndex < _rows.Count;
    }

    public override object GetValue(int ordinal)
    {
        string columnName = GetName(ordinal);
        return _rows[_currentIndex][columnName];
    }

    public override int GetValues(object[] values)
    {
        int count = Math.Min(FieldCount, values.Length);
        for (int i = 0; i < count; i++)
        {
            values[i] = GetValue(i);
        }
        return count;
    }

    public override string GetName(int ordinal)
    {
        if (_rows.Count == 0) return string.Empty;
        return _rows[0].Keys.ElementAt(ordinal);
    }

    public override int GetOrdinal(string name)
    {
        if (_rows.Count == 0) return -1;
        var keys = _rows[0].Keys.ToList();
        return keys.IndexOf(name);
    }

    public override bool IsDBNull(int ordinal)
    {
        var value = GetValue(ordinal);
        return value == null || value == DBNull.Value;
    }

    public override object this[int ordinal] => GetValue(ordinal);
    public override object this[string name] => _rows[_currentIndex][name];
    public override int FieldCount => _rows.Count > 0 ? _rows[0].Count : 0;
    public override bool HasRows => _rows.Count > 0;
    public override int Depth => 0;
    public override bool IsClosed => !_isOpen;
    public override int RecordsAffected => -1;

    public override bool GetBoolean(int ordinal) => Convert.ToBoolean(GetValue(ordinal));
    public override byte GetByte(int ordinal) => Convert.ToByte(GetValue(ordinal));
    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => 0;
    public override char GetChar(int ordinal) => Convert.ToChar(GetValue(ordinal));
    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => 0;
    public override string GetDataTypeName(int ordinal) => GetValue(ordinal).GetType().Name;
    public override DateTime GetDateTime(int ordinal) => Convert.ToDateTime(GetValue(ordinal));
    public override decimal GetDecimal(int ordinal) => Convert.ToDecimal(GetValue(ordinal));
    public override double GetDouble(int ordinal) => Convert.ToDouble(GetValue(ordinal));
    public override Type GetFieldType(int ordinal) => GetValue(ordinal).GetType();
    public override float GetFloat(int ordinal) => Convert.ToSingle(GetValue(ordinal));
    public override Guid GetGuid(int ordinal) => (Guid)GetValue(ordinal);
    public override short GetInt16(int ordinal) => Convert.ToInt16(GetValue(ordinal));
    public override int GetInt32(int ordinal) => Convert.ToInt32(GetValue(ordinal));
    public override long GetInt64(int ordinal) => Convert.ToInt64(GetValue(ordinal));
    public override string GetString(int ordinal) => GetValue(ordinal).ToString()!;

    public override IEnumerator GetEnumerator() => new DbEnumerator(this);
    
    public override bool NextResult() => false;
    public override void Close() => _isOpen = false;
}
