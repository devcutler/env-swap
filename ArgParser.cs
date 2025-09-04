internal class ArgParser(string[] args)
{
	private readonly string[] _args = args;
	private int _index = 0;

	public bool HasMore => _index < _args.Length;
	public string? Current => _index < _args.Length ? _args[_index] : null;

	public string? Consume()
	{
		if (_index < _args.Length)
		{
			return _args[_index++];
		}
		return null;
	}

	public string? Peek(int offset = 0)
	{
		int peekIndex = _index + offset;
		return peekIndex < _args.Length ? _args[peekIndex] : null;
	}

	public bool TryConsume(out string value)
	{
		if (_index < _args.Length)
		{
			value = _args[_index++];
			return true;
		}
		value = string.Empty;
		return false;
	}

	public bool Contains(string arg)
	{
		return Array.IndexOf(_args, arg, _index) >= 0;
	}

	public string[] Expect(int count = 1)
	{
		if (_index + count > _args.Length)
		{
			throw new ArgumentException($"Expected {count} argument(s)");
		}

		var result = new string[count];
		for (int i = 0; i < count; i++)
		{
			result[i] = _args[_index + i];
		}
		return result;
	}

	public void Skip(int count = 1)
	{
		_index += count;
	}
}