//ws://192.168.1.12/ws - GET
var setAngle = {"command":"setAngle","value":33.21767486553768}
var setAngle2 = {"command":"setAngle","value":0.24803212815649545}
var response = {"command":"angle","angle":0.0,"req_angle":0.2,"az_angle":23.0,"dac":2104};

//http://192.168.1.12/settings - GET
var settings_angle_response = {
    "sweep": 240,
    "max_sweep": 240.00,
    "azimuth": 23,
    "lat": 48.490677,
    "lng": 35.368084,
    "ip": "192.168.1.12",
    "sn": "SZR-4CDCD0D8CBB0",
    "version": "ROTATOR-SZ-1M; HW:1.0; FW:1.11",
    "reverse": 1,
    "c_n_180": 4150,
    "c_n_90": 3135,
    "c_0": 2107,
    "c_90": 1066,
    "c_180": 0
}

//http://192.168.1.12/settings/angle - POST
var settings_angle_payload = { "sweep": 240, "azimuth": 23 }
var settings_angle_response = { "ok": true }

//http://192.168.1.12/settings/geo - POST
var settings_geo_payload = { "lat": 49, "lng": 35.368084 };
var settings_geo_response = { "ok": true };

//http://192.168.1.12/settings/angle - POST
var settings_angle_payload = { "sweep": 230, "azimuth": 23 };
var settings_angle_response = { "ok": true };

//http://192.168.1.12/settings/ip - POST
var settings_ip_payload = { "ip": "192.168.1.11" };
var settings_ip_response = { "ok": true };

//http://192.168.1.12/settings/cal - POST
var settings_cal_payload = { "c_n_180": 4150, "c_n_90": 3135, "c_0": 0, "c_90": -3135, "c_180": -4150 };
var settings_cal_response = { "ok": true };