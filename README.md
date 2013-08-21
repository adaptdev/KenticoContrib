# KenticoContrib

KenticoContrib is a collection of extensions for [Kentico CMS](http://kentico.com).

## How to Build

```
git clone git@github.com:adaptdev/KenticoContrib.git
cd KenticoContrib
./build.bat build
```
Distribution packages can be found in the `/dist` directory after a successful build.

## How to Install into Kentico

1. Find the package for the component you want to install in the `/dist` directory, or use `KenticoContrib.zip` to install all KenticoContrib components.
2. Copy the package into the `/CMSSiteUtils/Import` directory of your Kentico site, or upload the package directly via the import wizard.
3. Use Kentico's [import wizard](http://devnet.kentico.com/docs/devguide/index.html?importing_a_site_or_objects.htm) to import the package into your Kentico site. __Make sure the _Import files_ option is selected__.