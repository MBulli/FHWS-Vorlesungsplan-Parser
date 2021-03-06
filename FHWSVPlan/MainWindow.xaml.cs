﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FHWSVPlan
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            VPlanParser p = new VPlanParser(browser);
            p.Start((vplan) =>
            {
                string iCalStr = iCalendarGenerator.CreateCalendarString(vplan);
                File.WriteAllText(string.Format("{0}.ics", vplan.Header.Name), iCalStr);
            });
        }
    }
}
