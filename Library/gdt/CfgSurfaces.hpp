class CfgSurfaces
{
	class Default
	{
	};
	class arm_dirt: Default
	{
		access=2;
		files="arm_dirt_*";
		character="empty";
		soundEnviron="dirt";
		soundHit="hard_ground";
		rough=0.38;
		maxSpeedCoef=0.89999998;
		dust=0.75;
		lucidity=2;
		grassCover=0.1;
		impact="hitGroundSoft";
		surfaceFriction=1.75;
	};
	class arm_forest: Default
	{
		access=2;
		files="arm_forest_*";
		character="arm_forest_clutter";
		soundEnviron="grass";
		soundHit="soft_ground";
		rough=0.079999998;
		maxSpeedCoef=0.89999998;
		dust=0.75;
		lucidity=4;
		grassCover=0.1;
		impact="hitGroundSoft";
		surfaceFriction=1.7;
	};
	class arm_wetland: Default
	{
		access=2;
		files="arm_wetland_*";
		character="arm_wetland_clutter";
		soundEnviron="grass";
		soundHit="soft_ground";
		rough=0.079999998;
		maxSpeedCoef=0.89999998;
		dust=0.55000001;
		lucidity=4;
		grassCover=0.89999998;
		impact="hitGroundSoft";
		surfaceFriction=1.7;
	};
	class arm_grassshort: Default
	{
		access=2;
		files="arm_grassshort_*";
		character="arm_grassshort_clutter";
		soundEnviron="grass";
		soundHit="soft_ground";
		rough=0.079999998;
		maxSpeedCoef=0.89999998;
		dust=0.55000001;
		lucidity=4;
		grassCover=0.89999998;
		impact="hitGroundSoft";
		surfaceFriction=1.7;
	};
	class arm_farmland: Default
	{
		access=2;
		files="arm_farmland_*";
		character="arm_farmland_clutter";
		soundEnviron="sand";
		soundHit="soft_ground";
		rough=0.079999998;
		maxSpeedCoef=0.89999998;
		dust=0.050000001;
		lucidity=4;
		grassCover=0.2;
		impact="hitGroundSoft";
		surfaceFriction=1.7;
	};
	class arm_sand: Default
	{
		access=2;
		files="arm_sand_*";
		character="empty";
		soundEnviron="sand";
		soundHit="hard_ground";
		rough=0.88;
		maxSpeedCoef=0.89999998;
		dust=0.75;
		lucidity=2;
		grassCover=0.1;
		impact="hitGroundSoft";
		surfaceFriction=1.75;
	};
	class arm_rock: Default
	{
		access=2;
		files="arm_rock_*";
		character="arm_rock_clutter";
		soundEnviron="grass";
		soundHit="soft_ground";
		rough=0.079999998;
		maxSpeedCoef=0.89999998;
		dust=0.75;
		lucidity=2;
		grassCover=0.1;
		impact="hitGroundSoft";
		surfaceFriction=1.75;
	};
	class arm_concrete: Default
	{
		access=2;
		files="arm_concrete_*";
		character="empty";
		soundEnviron="concrete";
		soundHit="concrete";
		rough=0.050000001;
		maxSpeedCoef=1;
		dust=0.15000001;
		lucidity=0.30000001;
		grassCover=0;
		impact="hitConcrete";
	};
};
class CfgSurfaceCharacters
{
	class arm_forest_clutter
	{
		probability[]={0.2,0.35,0.2,0.09,0.02,0.03,0.07,0.04};
		names[]=
		{
			"arm_WeedGreenTall",
			"arm_GrassGreen",
			"arm_WeedBrownTallGroup",
			"arm_FlowerCakile",
			"arm_FlowerLowYellow2",
			"arm_Grass_flower1",
			"arm_Fern",
			"arm_FernTall"
		};
	};
	class arm_wetland_clutter
	{
		probability[]={0.4,0.1};
		names[]=
		{
			"arm_GrassDryTall",
			"arm_GrassDryGroup"
		};
	};
	class arm_grassshort_clutter
	{
		probability[]={1};
		names[]=
		{
			"arm_GrassGreenGroup"
		};
	};
	class arm_farmland_clutter
	{
		probability[]={0.5,0.3,0.2};
		names[]=
		{
			"arm_GrassTall",
			"arm_GrassTall2",
			"arm_GrassTall3"
		};
	};
	class arm_rock_clutter
	{
		probability[]={0.2,0.7,0.1};
		names[]=
		{
			"arm_rock_stones",
			"arm_GrassGreen",
			"arm_GrassDead"
		};
	};
};