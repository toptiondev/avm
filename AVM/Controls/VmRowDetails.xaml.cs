using AVM.Helpers;
using AVM.Services;
using AVM.ViewModels.Controls;
using Azure.ResourceManager.Compute;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Diagnostics;

namespace AVM.Controls;

public partial class VmRowDetails : ContentView, IDisposable
{
    public static readonly BindableProperty VmNameProperty = BindableProperty.Create(
               propertyName: nameof(VmName),
               returnType: typeof(string),
               declaringType: typeof(VmRowDetails),
               defaultValue: "",
               defaultBindingMode: BindingMode.OneWay,
               propertyChanged: OnVirtualMachineNameChanged);

    public static readonly BindableProperty GroupProperty = BindableProperty.Create(
               propertyName: nameof(Group),
               returnType: typeof(string),
               declaringType: typeof(VmRowDetails),
               defaultValue: "",
               defaultBindingMode: BindingMode.OneWay,
               propertyChanged: OnResourceGroupChanged);

    public static readonly BindableProperty IsBusyProperty = BindableProperty.Create(
                      propertyName: nameof(IsBusy),
                      returnType: typeof(bool),
                      declaringType: typeof(VmRowDetails),
                      defaultValue: false,
                      defaultBindingMode: BindingMode.TwoWay);

    /// <summary>
    /// Virtual Machine Name
    /// </summary>
    public string? VmName
    {
        get
        {
            return (string?)GetValue(VmNameProperty);
        }
        set
        {
            SetValue(VmNameProperty, value);
        }
    }
    /// <summary>
    /// Virtual Machine Resource Group
    /// </summary>
    public string? Group
    {
        get
        {
            return (string?)GetValue(GroupProperty);
        }
        set
        {
            SetValue(GroupProperty, value);
        }
    }

    public bool IsBusy
    {
        get
        {
            return (bool)GetValue(IsBusyProperty);
        }
        set
        {
            SetValue(IsBusyProperty, value);
        }
    }

    List<IDisposable> _subs = new List<IDisposable>();

    private static void OnVirtualMachineNameChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var ctx = bindable as VmRowDetails;
        ctx.VmName = (string)newValue;
    }

    private static void OnResourceGroupChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var ctx = bindable as VmRowDetails;
        ctx.Group = (string)newValue;
    }

    Timer? _timer;
    readonly VmManager? _vmManager;

    public VmRowDetails()
    {
        _vmManager = AppServiceProvider.Current.GetService<VmManager>();
        InitializeComponent();
        this.Loaded += VmRowDetails_Loaded;

        if (_vmManager != null)
        {
            _subs.Add(_vmManager.OperationRunning.Subscribe(x =>
            {
                if (!string.IsNullOrEmpty(x.VmId))
                {
                    var ctx = (VirtualMachineResource)BindingContext;
                    if (x.VmId == ctx.Data.Name)
                    {
                        IsBusy = x.Status == VmOperation.Running;
                    }
                }
            }));
            _subs.Add(_vmManager.OperationCompleted.Subscribe(x =>
            {
                if (!string.IsNullOrEmpty(x.VmId))
                {
                    var ctx = (VirtualMachineResource)BindingContext;
                    if (x.VmId == ctx.Data.Name)
                    {
                        _ = LoadDetails();
                    }
                }
            }));
        }

        _ = LoadDetails();
    }

    private void VmRowDetails_Loaded(object? sender, EventArgs e)
    {
        _ = LoadDetails();
    }


    public async Task LoadDetails()
    {
        if (string.IsNullOrEmpty(VmName) || string.IsNullOrEmpty(Group)) return;
        if (_vmManager == null) return;

        var vm = await _vmManager.GetVirtualMachineMyId(VmName, Group);
        if (vm == null) return;

        this.BindingContext = vm;

        if (_timer != null)
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
            _timer.Dispose();
        }

        _timer = new Timer(async (state) =>
        {
            if (_vmManager == null) return;
            var vm = await _vmManager.GetVirtualMachineMyId(VmName, Group);
            if (vm != null)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    this.BindingContext = vm;
                });
            }
        }, null, 0, 2000);

    }

    public void Dispose()
    {
        foreach (var sub in _subs)
        {
            sub.Dispose();
        }

        if (_timer != null)
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
            _timer.Dispose();
        }
    }

    /// <summary>
    /// Launch RDP
    /// </summary>
    private void Button_Clicked(object sender, EventArgs e)
    {
        if(_vmManager != null)
        {
            var ctx = (VirtualMachineResource)BindingContext;
            _ = _vmManager.LaunchRdpClient(ctx.Data.Name, ctx.Data.Id.ResourceGroupName);
        }
    }

    /// <summary>
    /// Start Virtual Machine
    /// </summary>
    private void Button_Clicked_1(object sender, EventArgs e)
    {
        if (_vmManager != null)
        {
            var ctx = (VirtualMachineResource)BindingContext;
            _ = _vmManager.StartVm(ctx.Data.Name, ctx.Data.Id.ResourceGroupName);
        }
    }

    /// <summary>
    /// Restart Virtual Machine
    /// </summary>
    private void Button_Clicked_2(object sender, EventArgs e)
    {
        if (_vmManager != null)
        {
            var ctx = (VirtualMachineResource)BindingContext;
            _ = _vmManager.RestartVm(ctx.Data.Name, ctx.Data.Id.ResourceGroupName);
        }
    }

    /// <summary>
    /// Stop Virtual Machine
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Button_Clicked_3(object sender, EventArgs e)
    {
        if (_vmManager != null)
        {
            var ctx = (VirtualMachineResource)BindingContext;
            _ = _vmManager.StopVm(ctx.Data.Name, ctx.Data.Id.ResourceGroupName);
        }
    }

    /// <summary>
    /// Delete Virtual Machine
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Button_Clicked_4(object sender, EventArgs e)
    {
        if (_vmManager != null)
        {
            var ctx = (VirtualMachineResource)BindingContext;
            _ = _vmManager.DeleteVm(ctx.Data.Name, ctx.Data.Id.ResourceGroupName);
        }
    }
}