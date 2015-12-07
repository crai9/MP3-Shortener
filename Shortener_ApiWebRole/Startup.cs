using Owin;

namespace Shortener_ApiWebRole
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            //Configure the app from the StartUp.Auth.cs
            ConfigureAuth(app);
        }
    }
}
