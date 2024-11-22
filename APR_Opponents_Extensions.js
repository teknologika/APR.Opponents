// How the overrides work https://stackoverflow.com/questions/10427708/override-function-e-g-alert-and-call-the-original-function


// Returns for the race position the driver's relative gap to the camera car when available
driverrelativegaptoplayer = (function (originalFunction) {
    return function (raceposition) {
        var value;
        var index = ((raceposition) ?? '00').toString().padStart(2, '0');
        if ($prop('APROpponentsPlugin.GameIsSupported') != false && $prop('APROpponentsPlugin.OverrideJavaScriptFunctions') === true) {

            value = $prop('APROpponentsPlugin.Driver_' + index + '_GapToPlayer')
            //value = $prop('APROpponentsPlugin.GetPlayerLeaderboardPosition');
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
        if ($prop('APROpponentsPlugin.GameIsSupported') != false && $prop('APROpponentsPlugin.OverrideJavaScriptFunctions') === true) {
            value = $prop('APROpponentsPlugin.GetPlayerLeaderboardPosition');
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

// Returns the leaderboard position of the player's ahead/behind on track opponents
// relativeIndex 0 is the camera car, -1 the first driver ahead, 1 the first driver behind
// this function assumes the override check has already been performed
function apr_getopponentleaderboardposition_aheadbehind(relativeIndex) {
    var value = -1;
    var index = (Math.abs(relativeIndex) ?? '00').toString().padStart(2, '0');

    if (relativeIndex < 0) {

        value = $prop('APROpponentsPlugin.Driver_Ahead_' + index + '_LeaderboardPosition');
        //value = 'APROpponentsPlugin.Driver_Ahead_' + (relativeIndex ?? '00').toString().padStart(2, '0') + '_LeaderboardPosition';
    }
    else if (relativeIndex > 0) {
        value = $prop('APROpponentsPlugin.Driver_Behind_' + index + '_LeaderboardPosition');
    }
    else if (relativeIndex == 0) {
        value = getplayerleaderboardposition();
    }
    else {
        value = null;
    }
    return value;
}

getopponentleaderboardposition_aheadbehind = (function (originalFunction) {
    return function (relativeIndex) {
        var value;
        if ($prop('APROpponentsPlugin.GameIsSupported') != false && $prop('APROpponentsPlugin.OverrideJavaScriptFunctions') === true) {
            value = apr_getopponentleaderboardposition_aheadbehind(relativeIndex);
        }
        else {
            value = originalFunction(relativeIndex);
        }

        if (value != null) {
            return value;
        }
        else {
            return "";
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