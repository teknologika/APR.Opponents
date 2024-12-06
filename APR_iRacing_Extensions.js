// How the overrides work https://stackoverflow.com/questions/10427708/override-function-e-g-alert-and-call-the-original-function

// Functions for flags

// Returns the black flag status for a driver if available 
function driverflagblack(raceposition) {
    var value;
    var index = ((raceposition) ?? '00').toString().padStart(2, '0');
    if ($prop('APRiRacing.GameIsSupported') != false && $prop('APRiRacing.OverrideJavaScriptFunctions') === true) {
        value = $prop('APRiRacing.Driver_' + index + 'FlagBlack')
    }
    if (value != null) {
        return value;
    }
    else {
        return "";
    }
}

// Returns the furled black flag status for a driver if available (e.g. slowdown, penalty to serve)
function driverflagblackfurled(raceposition) {
    var value;
    var index = ((raceposition) ?? '00').toString().padStart(2, '0');
    if ($prop('APRiRacing.GameIsSupported') != false && $prop('APRiRacing.OverrideJavaScriptFunctions') === true) {
        value = $prop('APRiRacing.Driver_' + index + 'FlagBlackFurled')
    }
    if (value != null) {
        return value;
    }
    else {
        return "";
    }
}

// Returns the repair (meatball) flag status for a driver if available
function driverflagrepair(raceposition) {
    var value;
    var index = ((raceposition) ?? '00').toString().padStart(2, '0');
    if ($prop('APRiRacing.GameIsSupported') != false && $prop('APRiRacing.OverrideJavaScriptFunctions') === true) {
        value = $prop('APRiRacing.Driver_' + index + '_FlagRepair')
    }
    if (value != null) {
        return value;
    }
    else {
        return "";
    }
}

// Returns for the race position the driver's relative gap to the camera car when available
driverrelativegaptoplayer = (function (originalFunction) {
    return function (raceposition) {
        var value;
        var index = ((raceposition) ?? '00').toString().padStart(2, '0');
        if ($prop('APRiRacing.GameIsSupported') != false && $prop('APRiRacing.OverrideJavaScriptFunctions') === true) {

            value = $prop('APRiRacing.Driver_' + index + '_GapToPlayer')
        }
        else {
            value = originalFunction(raceposition);
        }

        if (value != null) {
            return value;
        }
        else {
            return "";
        }
    };
})(driverrelativegaptoplayer);

// Returns the current leaderboard position of the camera car
getplayerleaderboardposition = (function (originalFunction) {
    return function (leaderboardPosition) {
        var value;
        if ($prop('APRiRacing.GameIsSupported') != false && $prop('APRiRacing.OverrideJavaScriptFunctions') === true) {
            value = $prop('APRiRacing.GetPlayerLeaderboardPosition');
        }
        else {
            value = originalFunction(leaderboardPosition);
        }

        if (value != null) {
            return value;
        }
        else {
            return "";
        }
    };
})(getplayerleaderboardposition);


// Returns the leaderboard position of the player's ahead/behind position
// relativeIndex 0 is the camera car, -1 is one position ahead, 1 one position behind behind
// this function assumes the override check has already been performed
function getopponentleaderboardposition_positionaheadbehind(relativeIndex) {
    var positon = getplayerleaderboardposition();
    if (positon != null) {
        return positon + relativeIndex;
    }
    else {
        return positon;
    }
}


// Returns the leaderboard position of the player's ahead/behind on track opponents
// relativeIndex 0 is the camera car, -1 the first driver ahead, 1 the first driver behind
// this function assumes the override check has already been performed
function apr_getopponentleaderboardposition_aheadbehind(relativeIndex) {
    var index;
    if (relativeIndex < 0)
        index = '-' + (Math.abs(relativeIndex) ?? '00').toString().padStart(2, '0');
    else
        index = (relativeIndex ?? '00').toString().padStart(2, '0');
    return $prop('APRiRacing.Relative_' + index + '_SimhubPosition');
}

// Returns the leaderboard position of the player's ahead/behind on track opponents
// relativeIndex 0 is the camera car, -1 the first driver ahead, 1 the first driver behind
// this function assumes the override check has already been performed
function drivernamecolor_aheadbehind(relativeIndex) {
    var index;
    if (relativeIndex < 0)
        index = '-' + (Math.abs(relativeIndex) ?? '00').toString().padStart(2, '0');
    else
        index = (relativeIndex ?? '00').toString().padStart(2, '0');
    return $prop('APRiRacing.Relative_' + index + '_NameColor');
}


getopponentleaderboardposition_aheadbehind = (function (originalFunction) {
    return function (relativeIndex) {
        var value;
        if ($prop('APRiRacing.GameIsSupported') != false && $prop('APRiRacing.OverrideJavaScriptFunctions') === true) {
            value = apr_getopponentleaderboardposition_aheadbehind(relativeIndex);
        }
        else {
            value = originalFunction(relativeIndex);
        }

        if (value != null) {
            return value;
        }
        else {
            return null;
        }
    };
})(getopponentleaderboardposition_aheadbehind);

driverbestlap = (function (originalFunction) {
    return function (leaderboardPosition) {
        var value = originalFunction(leaderboardPosition);
        if (value == "00:00:00") {
            return '--:--.---';
        }
        else {
            return value;
        }
    };
})(driverbestlap);

driverlastlap = (function (originalFunction) {
    return function (leaderboardPosition) {
        var value = originalFunction(leaderboardPosition);
        if (value == "00:00:00") {
            return '--:--.---';
        }
        else {
            return value;
        }
    };
})(driverlastlap);