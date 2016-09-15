using System.IO;

public class TestInput : IInput
{
    private readonly StreamReader sr;

    public TestInput(string fullPath)
    {
        sr = new StreamReader(new FileStream(fullPath, FileMode.Open));
    }

    #region Implementation of IInput

    public string GetInput()
    {
        return sr.ReadLine();
    }

    #endregion
}