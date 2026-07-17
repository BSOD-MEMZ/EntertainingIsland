using EntertainingIsland.ViewModels;

namespace EntertainingIsland.Views.WelcomePages;

public interface IWelcomePage
{
    WelcomeViewModel ViewModel { get; set; }
}
