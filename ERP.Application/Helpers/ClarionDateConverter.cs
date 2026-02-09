using System;

namespace ERP.Application.Helpers;

public static class ClarionDateConverter
{
    private static readonly DateTime BaseDate = new DateTime(1800, 12, 28);

    public static DateTime? ClarionIntToDate(int? value)
    {
        if (!value.HasValue || value.Value <= 0)
            return null;

        return BaseDate.AddDays(value.Value);
    }

    public static int? DateToClarionInt(DateTime? value)
    {
        if (!value.HasValue)
            return null;

        return (int)(value.Value.Date - BaseDate).TotalDays;
    }
}
