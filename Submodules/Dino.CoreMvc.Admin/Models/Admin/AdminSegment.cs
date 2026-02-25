using Dino.Common.Helpers;
using System;

namespace Dino.CoreMvc.Admin.Models.Admin
{
	public class AdminSegment
	{
		public AdminSegment()
		{
			Navigation = new AdminSegmentNavigation();
			UI = new AdminSegmentUI();
			General = new AdminSegmentGeneral();
			UI.ShowInMenu = true;
			General.Priority = 999;
		}

		
		public AdminSegmentNavigation Navigation { get; set; }
		public AdminSegmentUI UI { get; set; }
		public AdminSegmentGeneral General { get; set; }
	}

	public class AdminSegmentNavigation
	{
		public string ControllerName { get; set; }
		public string CustomPath { get; set; }
	}

	public class AdminSegmentUI
	{
		public string Icon { get; set; }
		public IconType? IconType { get; set; }
		public string IconFamily { get; set; }
		public bool ShowInMenu { get; set; }
	}

	public class AdminSegmentGeneral
	{
		public string Id { get; set; }
		public string Name { get; set; }
		public bool IsSettings { get; set; } = false;
		public bool IsGeneric { get; set; } = true;
		public double Priority { get; set; }
		public string MenuHeader { get; set; } // Optional header for grouping segments under custom submenu headers
	}
}
