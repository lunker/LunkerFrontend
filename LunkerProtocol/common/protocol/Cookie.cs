public struct Cookie
{
    // not yet 
    int value;
    
    public Cookie(int value)
    {
        this.value = value;
    }

    public Cookie(string value)
    {
        this.value = int.Parse(value);
    }

    public int Value
    {
        get { return this.value; }
    }
}