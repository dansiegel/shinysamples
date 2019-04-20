﻿using ReactiveUI.Fody.Helpers;
using Shiny.Sensors;
using System;
using System.Reactive.Disposables;

namespace Samples.Sensors
{
    public class CompassViewModel : ViewModel
    {
        readonly ICompass compass;
        public CompassViewModel(ICompass compass)
            => this.compass = compass;


        [Reactive] public double Heading { get; private set; }


        public override void OnAppearing()
        {
            base.OnAppearing();
            this.compass
                .WhenReadingTaken()
                .Subscribe(x => this.Heading = x.MagneticHeading)
                .DisposeWith(this.DeactivateWith);
        }
    }
}
