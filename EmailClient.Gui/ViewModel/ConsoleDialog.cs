using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using EmailClient.Gui.Collection;

namespace EmailClient.Gui.ViewModel;

public class StringWriterExt : StringWriter
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public delegate void FlushedEventHandler(object sender, EventArgs args);
    public event FlushedEventHandler Flushed;
    public virtual bool AutoFlush { get; set; }

    public StringWriterExt()
        : base() { }

    public StringWriterExt(bool autoFlush)
        : base() { this.AutoFlush = autoFlush; }

    protected void OnFlush()
    {
        var eh = Flushed;
        if (eh != null)
            eh(this, EventArgs.Empty);
    }

    public override void Flush()
    {
        base.Flush();
        OnFlush();
    }

    public override void Write(char value)
    {
        base.Write(value);
        if (AutoFlush) Flush();
    }

    public override void Write(string value)
    {
        base.Write(value);
        if (AutoFlush) Flush();
    }

    public override void Write(char[] buffer, int index, int count)
    {
        base.Write(buffer, index, count);
        if (AutoFlush) Flush();
    }
}

public partial class ConsoleOutputViewModel : ObservableObject, INotifyPropertyChanged
{
    [ObservableProperty]
    private StringWriterExt _sw;
    private string _consoleOut;
    public string ConsoleOut
    {
        get { return _consoleOut; }
        set
        {
            if (_consoleOut != value)
            {
                _consoleOut = value;
                OnPropertyChanged(nameof(ConsoleOut));
            }
        }
    }

    public ConsoleOutputViewModel()
    {
        _sw = new StringWriterExt(true);
        Console.SetOut(_sw);
        Console.SetError(_sw);
        _sw.Flushed += (s, a) => ConsoleOut = _sw.ToString();
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
