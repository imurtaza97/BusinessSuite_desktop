using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using BusinessSuite.BLL.Services;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using Avalonia.Threading;
using BusinessSuite.DAL.Data;
using BusinessSuite.UI.ViewModels;
using BusinessSuite.UI.Views;
using QuestPDF.Infrastructure;

namespace BusinessSuite.UI;

// Define a simple factory class to avoid complex DI logic for now
public class SimpleDbContextFactory : IDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext() => new AppDbContext();
}

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Use Post to ensure this runs on the UI Thread
            Dispatcher.UIThread.Post(() => 
            {
                QuestPDF.Settings.License = LicenseType.Community;
                
                var db = new AppDbContext();
                db.Database.Migrate();

                bool licenseExists = db.LicenseActivations.Any(la => la.IsValid);
                bool businessExists = db.Businesses.Any();

                if (!licenseExists)
                {
                    var agreementVm = new LegalAgreementViewModel();
                    var agreementWin = new LegalAgreementView { DataContext = agreementVm };
                    agreementVm.OnAccepted += () => 
                    {
                        var dbFactory = new SimpleDbContextFactory();
                        desktop.MainWindow = new ActivationForm 
                        { 
                            DataContext = new ActivationFormViewModel(dbFactory, new ActivationService(dbFactory)) 
                        };
                        desktop.MainWindow.Show();
                        agreementWin.Close();
                    };
                    desktop.MainWindow = agreementWin;
                }
                else if (!businessExists)
                {
                    var dbFactory = new SimpleDbContextFactory();
                    var registerService = new BusinessSuite.BLL.Services.RegisterService(dbFactory);
                    desktop.MainWindow = new RegisterForm
                    {
                        DataContext = new RegisterFormViewModel(registerService)
                    };
                }
                else
                {
                    desktop.MainWindow = new LoginForm
                    {
                        DataContext = new LoginFormViewModel(db)
                    };
                }
                
                desktop.MainWindow.Show();
            });
        }

        base.OnFrameworkInitializationCompleted();
    }
}