using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.Components;
using System.Collections.Generic;
using ScoreList.Scores;
using UnityEngine.UI;
using System.Linq;
using System;

namespace ScoreList.UI {
    public class FilterListCellWrapper
    {
        private FilterViewController controller;

        [UIValue("name")]
        public string name { get; set; }

        [UIValue("data")]
        public string data { get; set; }

        [UIAction("DeleteFilter")]
        internal void DeleteFilter()
        {
            var filter = controller.filters.Find(f => f.Name == name);
            controller.filters.Remove(filter);

            controller.list.data.Remove(this);
            controller.list.tableView.ReloadData();

            controller.ToggleNoFiltersText(true);
        }

        public FilterListCellWrapper(FilterViewController controller, string name, string data) 
        {
            this.controller = controller;
            this.name = name;
            this.data = data;
        }
    }

    [HotReload(RelativePathToLayout = @"Views\ScoreFilters.bsml")]
    [ViewDefinition("ScoreList.UI.Views.ScoreFilters.bsml")]
    public class FilterViewController : BSMLAutomaticViewController
    {
        // components

        [UIComponent("list")] public CustomCellListTableData list;
        public List<Filter> filters = new List<Filter>();

        // sort components

        [UIComponent("sort")] public DropDownListSetting sort;
        [UIComponent("order")] public DropDownListSetting order;
        [UIComponent("no-filters-text")] public LayoutElement noFiltersText;

        // star components

        [UIComponent("filter-stars-minimum")] public SliderSetting filterStarsMinimum;
        [UIComponent("filter-stars-maximum")] public SliderSetting filterStarsMaximum;

        // date components

        [UIComponent("filter-date-after-month")] public SliderSetting filterDateAfterMonth;
        [UIComponent("filter-date-after-year")] public SliderSetting filterDateAfterYear;

        [UIComponent("filter-date-before-month")] public SliderSetting filterDateBeforeMonth;
        [UIComponent("filter-date-before-year")] public SliderSetting filterDateBeforeYear;

        // misses components

        [UIComponent("filter-misses-minimum")] public SliderSetting filterMissesMinimum;
        [UIComponent("filter-misses-maximum")] public SliderSetting filterMissesMaximum;

        // accuracy components

        [UIComponent("filter-accuracy-minimum")] public SliderSetting filterAccuracyMinimum;
        [UIComponent("filter-accuracy-maximum")] public SliderSetting filterAccuracyMaximum;


        [UIAction("#post-parse")]
        public void SetupUI() {
            list.data.Clear();
        }

        internal void ToggleNoFiltersText(bool value) => noFiltersText.gameObject.SetActive(value);

        // main functions

        [UIAction("ApplyFilters")]
        internal void ApplyFilters()
        {
            var query = new SearchQuery
            {
                Order = (string)order.Value,
                SortBy = (string)sort.Value,
                Filters = filters
            };

            Coordinator.Instance.ShowFilteredScores(query);
        }

        [UIAction("FilterSelect")]
        internal void FilterSelect(object _, int index) => filterChoice = _filterChoices[index];

        [UIAction("ResetFilters")]
        internal void ResetFilters() {
            filters.Clear();
            list.data.Clear();
            list.tableView.ReloadData();

            sort.Value = "PP";
            order.Value = "DESC";

            ToggleNoFiltersText(true);
        }

        private Filter TryCreateFilter() {
            // less messy
            Filter currentFilter = null;
            switch (filterChoice) {
                case "Stars":
                    float? starsMaximum = filterStarsMaximum.Value != maxStars ? filterStarsMaximum.Value : null;
                    float? starsMinimum = filterStarsMinimum != 0 ? filterStarsMinimum.Value : null;

                    if (starsMaximum != null || starsMinimum != null)
                        currentFilter = new StarsFilter(starsMinimum, starsMaximum);
                    break;

                case "Date":
                    int? dateAfterMonth = filterDateAfterMonth.Value != 1 ? (int)filterDateAfterMonth.Value : null;
                    int? dateAfterYear = filterDateAfterYear.Value != 2018 ? (int)filterDateAfterYear.Value : null;

                    int? dateBeforeMonth = filterDateBeforeMonth.Value != maxMonth ? (int)filterDateBeforeMonth.Value : null;
                    int? dateBeforeYear = filterDateBeforeYear.Value != maxYear ? (int)filterDateBeforeYear.Value : null;

                    string after = 
                    (dateAfterMonth != null || dateAfterYear != null) ? after = $"{dateAfterYear ?? 2018}-{dateAfterMonth ?? 1}-1" : null;                  

                    string before = 
                    (dateBeforeYear != null || dateBeforeMonth != null) ? before = $"{dateBeforeYear ?? 2018}-{dateBeforeMonth ?? 1}-1" : null;

                    if (after != null || before != null)
                        currentFilter = new DateFilter(after, before);
                    break;

                case "Misses":
                    int? missesMaximum = filterMissesMaximum.Value != 100 ? (int)filterMissesMaximum.Value : null;
                    int? missesMinimum = filterMissesMinimum.Value != 0 ? (int)filterMissesMinimum.Value : null;

                    if (missesMaximum != null || missesMinimum != null)
                        currentFilter = new MissesFilter(missesMinimum, missesMaximum);
                    break;

                case "Accuracy":
                    float? accuracyMinimum = filterAccuracyMinimum.Value != 0f ? filterAccuracyMinimum.Value : null;
                    float? accuracyMaximum = filterAccuracyMaximum.Value != 100f ? filterAccuracyMaximum.Value;

                    if (accuracyMaximum != null || accuracyMinimum != null)
                        currentFilter = new AccuracyFilter(accuracyMinimum, accuracyMaximum);
                    break;
            }

            return currentFilter;
        }

        [UIAction("CreateFilter")]
        internal void CreateFilter()
        {
            if (filters.Any(f => f.Name == filterChoice)) return;

            var filter = TryCreateFilter();
            if (filter == null) return;

            var (start, end) = filter.GetValues();
            string value =
            ((start == null || end == null) ? (start != null ? $"Bigger {start}" : $"Smaller {end}") : $"Bigger {start] Smaller {end}");

            var wrapper = new FilterListCellWrapper(this, filterChoice, value);

            filters.Add(filter);
            list.data.Add(wrapper);
            list.tableView.ReloadData();

            ToggleNoFiltersText(false);
        }

        // choice

        [UIValue("filter-choice")]
        public string filterChoice = "Stars";

        // choices

        [UIValue("order-choices")]
        public List<object> orderChoices = new List<object> {
            "DESC", "ASC"
        };

        [UIValue("sort-choices")]
        public List<object> sortChoices = new List<object> {
            "PP",
            "Stars",
            "Rank",
            "Accuracy",
            "TimeSet",
            "MissedNotes"
        };

        public List<string> _filterChoices = new List<string> {
            "Stars",
            "Accuracy",
            "Misses",
            "Date"
        };
        public Enum FilterTypes
        {
            Stars,
            Accuracy,
            Misses,
            Date
        }

        [UIValue("filter-choices")]
        public List<object> filterChoices => _filterChoices.Cast<object>().ToList();

        // formatters

        public List<string> monthChoices = new List<string> {
            "January ", "February", "March",
            "April",    "May",      "June",
            "July",     "August",   "September",
            "October",  "November", "December"
        };

        [UIAction("FormatMonth")]
        internal string FormatMonth(int index) => monthChoices[index - 1];

        // utils

        [UIValue("max-year")]
        public int maxYear = DateTime.Today.Year;

        [UIValue("max-month")]
        public int maxMonth = 12;

        [UIValue("max-stars")]
        public float maxStars = 14f;

        [UIValue("max-accuracy")]
        public int maxAccuracy = 100;

        [UIValue("max-misses")]
        public int maxMisses= 100;
    }
}
