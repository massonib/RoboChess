using Microsoft.Practices.Unity;
using StockChessCS.Interfaces;
using StockChessCS.Services;

namespace RoboChess.Helpers
{
    public class ViewModelLocator
    {

        private UnityContainer _container;
        public ViewModelLocator()
        {
            _container = new UnityContainer();
            _container.RegisterType<IEngineService, StockfishService>();
        }

        public MainViewModel MainVM
        {
            get { return _container.Resolve<MainViewModel>(); }
        }
    }
}
