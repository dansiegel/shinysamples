﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using Prism.Navigation;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Samples.Infrastructure;
using Shiny;
using Shiny.BluetoothLE;


namespace Samples.BluetoothLE
{
    public class AdapterViewModel : ViewModel
    {
        IDisposable scan;


        public AdapterViewModel(INavigationService navigator,
                                IDialogs dialogs,
                                IBleManager? bleManager = null)
        {
            this.CanControlAdapterState = bleManager?.CanControlAdapterState() ?? false;

            this.Select = ReactiveCommand.CreateFromTask<PeripheralItemViewModel>(vm =>
                navigator.Navigate("Peripheral", ("Peripheral", vm.Peripheral))
            );

            this.ToggleAdapterState = ReactiveCommand.CreateFromTask(
                async () =>
                {
                    if (bleManager == null)
                    {
                        await dialogs.Alert("Platform Not Supported");
                    }
                    else
                    {
                        var poweredOn = bleManager.Status == AccessState.Available;
                        if (!bleManager.TrySetAdapterState(!poweredOn))
                            await dialogs.Alert("Cannot change bluetooth adapter state");
                    }
                }
            );

            this.ScanToggle = ReactiveCommand.CreateFromTask(
                async () =>
                {
                    if (bleManager == null)
                    {
                        await dialogs.Alert("Platform Not Supported");
                        return;
                    }
                    if (this.IsScanning)
                    {
                        this.IsScanning = false;
                        this.scan?.Dispose();
                    }
                    else
                    {
                        this.Peripherals.Clear();

                        this.scan = bleManager
                            .Scan()
                            .Buffer(TimeSpan.FromSeconds(1))
                            .Synchronize()
                            .SubOnMainThread(
                                results =>
                                {
                                    var list = new List<PeripheralItemViewModel>();
                                    foreach (var result in results)
                                    {
                                        var peripheral = this.Peripherals.FirstOrDefault(x => x.Equals(result.Peripheral));
                                        if (peripheral == null)
                                            peripheral = list.FirstOrDefault(x => x.Equals(result.Peripheral));

                                        if (peripheral != null)
                                        {
                                            peripheral.Update(result);
                                        }
                                        else
                                        {
                                            peripheral = new PeripheralItemViewModel(result.Peripheral);
                                            peripheral.Update(result);
                                            list.Add(peripheral);
                                        }
                                    }
                                    if (list.Any())
                                        this.Peripherals.AddRange(list);
                                },
                                ex => dialogs.Alert(ex.ToString(), "ERROR")
                            )
                            .DisposeWith(this.DeactivateWith);

                        this.IsScanning = true;
                    }
                }
            );
        }


        public override void OnDisappearing()
        {
            base.OnDisappearing();
            this.IsScanning = false;
        }


        public ICommand Select { get; }
        public ICommand ScanToggle { get; }
        public ICommand ToggleAdapterState { get; }
        public bool CanControlAdapterState { get; }
        public ObservableList<PeripheralItemViewModel> Peripherals { get; } = new ObservableList<PeripheralItemViewModel>();
        [Reactive] public bool IsScanning { get; private set; }
    }
}