// THIS FILE IS GENERATED, ALL CHANGES WILL BE LOST

package com.pgssoft.meteopllibrary;

public interface Utils
    {
        // calculate timestamp for given month, day and (fractional) hour; year must be provided by client as it doesn't appear on source img
        long getTimestamp(int m, int d, double hour);

        // handle failure in image processing
        void throwException(String description);
    }
