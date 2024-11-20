// How the overrides work https://stackoverflow.com/questions/10427708/override-function-e-g-alert-and-call-the-original-function

/*
function getplayerleaderboardposition(){
    var value;
    if ($prop('APROpponentsPlugin.GameIsSupported') != false && $prop('APROpponentsPlugin.OverrideJavaScriptFunctions') === true) {
        value = apr_getplayerleaderboardposition();
    }
    else {
        value = getplayerleaderboardposition();
    }
    
    if (value != null) {
        return value;
    }
    else {
        return "";
    }
}
    */

function getopponentleaderboardposition_aheadbehind(difference) {
    var value = getplayerleaderboardposition() + difference;
    if (value < -1) {
        return -1;
    }
    else {
        return value;
    }
}

/*getplayerleaderboardposition = (function (originalFunction) {
    return function (leaderboardPosition) {
        return originalFunction(leaderboardPosition);
    };
})(getplayerleaderboardposition); */

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