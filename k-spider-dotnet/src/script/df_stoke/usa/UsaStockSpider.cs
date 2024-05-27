namespace k_spider_dotnet.script.df_stoke;

public class UsaStockSpider
{
}

// curl 'https://www.futunn.com/quote-api/quote-v2/get-stock-list?marketType=2&plateType=1&rankType=1&page=1&pageSize=50' \
//   -H 'Accept: application/json, text/plain, */*' \
//   -H 'Accept-Language: zh-CN,zh;q=0.9,en;q=0.8,en-GB;q=0.7,en-US;q=0.6' \
//   -H 'Connection: keep-alive' \
//   -H 'Cookie: cipher_device_id=1715273012247154; device_id=1715273012247154; _gcl_au=1.1.173194740.1715698037; csrfToken=PANsC570ivtIzYxothamiscz; locale=zh-cn; locale.sig=ObiqV0BmZw7fEycdGJRoK-Q0Yeuop294gBeiHL1Lqgq; Hm_lvt_f3ecfeb354419b501942b6f9caf8d0db=1715698033,1716819070; futunn_lang=zh-CN; _gid=GA1.2.968867191.1716819072; _clck=ad9ed9%7C2%7Cfm4%7C0%7C1595; 0_us_req_section=1; sensorsdata2015jssdkcross=%7B%22distinct_id%22%3A%22ftv1eF%2Fp3Xt6VArCNT6Tx9e84hdR3yzrJNOJAXd%2BAtajTWB5G0OLlL2IJh0l5xv81upW%22%2C%22first_id%22%3A%2218f5e3d14f9319-07ae878cd14b8-76574611-2359296-18f5e3d14fa40d%22%2C%22props%22%3A%7B%22%24latest_traffic_source_type%22%3A%22%E8%87%AA%E7%84%B6%E6%90%9C%E7%B4%A2%E6%B5%81%E9%87%8F%22%2C%22%24latest_search_keyword%22%3A%22%E6%9C%AA%E5%8F%96%E5%88%B0%E5%80%BC%22%2C%22%24latest_referrer%22%3A%22https%3A%2F%2Fcn.bing.com%2F%22%7D%2C%22identities%22%3A%22eyIkaWRlbnRpdHlfbG9naW5faWQiOiJmdHYxZUYvcDNYdDZWQXJDTlQ2VHg5ZTg0aGRSM3l6ckpOT0pBWGQrQXRhalRXQjVHME9MbEwySUpoMGw1eHY4MXVwVyIsIiRpZGVudGl0eV9jb29raWVfaWQiOiIxOGY1ZTNkMTRmOTMxOS0wN2FlODc4Y2QxNGI4LTc2NTc0NjExLTIzNTkyOTYtMThmNWUzZDE0ZmE0MGQifQ%3D%3D%22%2C%22history_login_id%22%3A%7B%22name%22%3A%22%24identity_login_id%22%2C%22value%22%3A%22ftv1eF%2Fp3Xt6VArCNT6Tx9e84hdR3yzrJNOJAXd%2BAtajTWB5G0OLlL2IJh0l5xv81upW%22%7D%2C%22%24device_id%22%3A%2218f5e3d14f9319-07ae878cd14b8-76574611-2359296-18f5e3d14fa40d%22%7D; _gat_gtag_UA_71722593_3=1; _gat_UA-71722593-2=1; Hm_lpvt_f3ecfeb354419b501942b6f9caf8d0db=1716825961; _ga_370Q8HQYD7=GS1.2.1716825910.5.1.1716825961.9.0.0; passport_dp_data=wfnO4ouVce7F6hGuqJc5sn4VgmFlU0eHKSDarFmszGb93WZS2gFMMYaY0uAzZoT3bzSmUmGJRlWA5I500F0JJsPZyMJ8Wt5QZ%2FAFY4fGFS8%3D; ftreport-jssdk%40session={%22distinctId%22:%22ftv1eF/p3Xt6VArCNT6Tx9e84sVt0bqYt3UdZ86CThgLCrF5G0OLlL2IJh0l5xv81upW%22%2C%22firstId%22:%22ftv1eF/p3Xt6VArCNT6Tx9e84otR8zIKxACvF+9jh1Rpub15G0OLlL2IJh0l5xv81upW%22%2C%22latestReferrer%22:%22https://www.futunn.com/%22}; _ga_FZ1PVH4G8R=GS1.1.1716825937.4.1.1716825964.0.0.0; _ga=GA1.1.1805342134.1715273013; _ga_K1RSSMGBHL=GS1.1.1716825938.4.1.1716825964.0.0.0; _uetsid=02bb59601c3311efb8b9e78a8b3bc8e6; _uetvid=dc29a000120011efa85b1b53db60ba28; _ga_NTZDYESDX1=GS1.2.1716825938.4.1.1716825964.34.0.0; _ga_EJJJZFNPTW=GS1.1.1716825910.5.1.1716825967.0.0.0; _ga_XECT8CPR37=GS1.1.1716825910.5.1.1716825967.3.0.0; _clsk=6rk71f%7C1716825969003%7C3%7C1%7Co.clarity.ms%2Fcollect' \
//   -H 'DNT: 1' \
//   -H 'Referer: https://www.futunn.com/quote?global_content=%7B%22promote_id%22%3A13766,%22sub_promote_id%22%3A2%7D' \
//   -H 'Sec-Fetch-Dest: empty' \
//   -H 'Sec-Fetch-Mode: cors' \
//   -H 'Sec-Fetch-Site: same-origin' \
//   -H 'User-Agent: Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36 Edg/125.0.0.0' \
//   -H 'futu-x-csrf-token: PANsC570ivtIzYxothamiscZ' \
//   -H 'quote-token: 7034c9e103' \
//   -H 'sec-ch-ua: "Microsoft Edge";v="125", "Chromium";v="125", "Not.A/Brand";v="24"' \
//   -H 'sec-ch-ua-mobile: ?0' \
//   -H 'sec-ch-ua-platform: "Windowns"'
//   
//   
//   curl 'https://www.futunn.com/quote-api/quote-v2/get-quote-minute?stockId=77571404549706&marketType=2&type=1&marketCode=11&instrumentType=3&subInstrumentType=0&req_section=2' \
//   -H 'Accept: application/json, text/plain, */*' \
//   -H 'Accept-Language: zh-CN,zh;q=0.9,en;q=0.8,en-GB;q=0.7,en-US;q=0.6' \
//   -H 'Connection: keep-alive' \
//   -H 'Cookie: cipher_device_id=1715273012247155; device_id=1715273012247155; _gcl_au=1.1.173194740.1715698037; csrfToken=PANsC570ivtIzYxothamiscZ; locale=zh-cn; locale.sig=ObiqV0BmZw7fEycdGJRoK-Q0Yeuop294gBeiHL1LqgQ; Hm_lvt_f3ecfeb354419b501942b6f9caf8d0db=1715698033,1716819070; futunn_lang=zh-CN; _gid=GA1.2.968867191.1716819072; _clck=ad9ed9%7C2%7Cfm4%7C0%7C1595; sensorsdata2015jssdkcross=%7B%22distinct_id%22%3A%22ftv1eF%2Fp3Xt6VArCNT6Tx9e84hdR3yzrJNOJAXd%2BAtajTWB5G0OLlL2IJh0l5xv81upW%22%2C%22first_id%22%3A%2218f5e3d14f9319-07ae878cd14b8-76574611-2359296-18f5e3d14fa40d%22%2C%22props%22%3A%7B%22%24latest_traffic_source_type%22%3A%22%E8%87%AA%E7%84%B6%E6%90%9C%E7%B4%A2%E6%B5%81%E9%87%8F%22%2C%22%24latest_search_keyword%22%3A%22%E6%9C%AA%E5%8F%96%E5%88%B0%E5%80%BC%22%2C%22%24latest_referrer%22%3A%22https%3A%2F%2Fcn.bing.com%2F%22%7D%2C%22identities%22%3A%22eyIkaWRlbnRpdHlfbG9naW5faWQiOiJmdHYxZUYvcDNYdDZWQXJDTlQ2VHg5ZTg0aGRSM3l6ckpOT0pBWGQrQXRhalRXQjVHME9MbEwySUpoMGw1eHY4MXVwVyIsIiRpZGVudGl0eV9jb29raWVfaWQiOiIxOGY1ZTNkMTRmOTMxOS0wN2FlODc4Y2QxNGI4LTc2NTc0NjExLTIzNTkyOTYtMThmNWUzZDE0ZmE0MGQifQ%3D%3D%22%2C%22history_login_id%22%3A%7B%22name%22%3A%22%24identity_login_id%22%2C%22value%22%3A%22ftv1eF%2Fp3Xt6VArCNT6Tx9e84hdR3yzrJNOJAXd%2BAtajTWB5G0OLlL2IJh0l5xv81upW%22%7D%2C%22%24device_id%22%3A%2218f5e3d14f9319-07ae878cd14b8-76574611-2359296-18f5e3d14fa40d%22%7D; _clsk=6rk71f%7C1716826442629%7C4%7C1%7Co.clarity.ms%2Fcollect; Hm_lpvt_f3ecfeb354419b501942b6f9caf8d0db=1716826502; _gat_UA-71722593-3=1; _ga_370Q8HQYD7=GS1.2.1716825910.5.1.1716826504.60.0.0; passport_dp_data=umFWbJxCHFDl3aWQZ4yyiaAaSHdSWHVNPQCOnyhF38i9D9Hev%2Fg5YPKx9KuHv2ymUPdBIxBJg7uVylpxSJLUlWKXoK9nabw3bKlsqrtGHBs%3D; ftreport-jssdk%40session={%22distinctId%22:%22ftv1eF/p3Xt6VArCNT6Tx9e84ggGNXxhO0Bbjqo2CD3mdqd5G0OLlL2IJh0l5xv81upW%22%2C%22firstId%22:%22ftv1eF/p3Xt6VArCNT6Tx9e84otR8zIKxACvF+9jh1Rpub15G0OLlL2IJh0l5xv81upW%22%2C%22latestReferrer%22:%22https://www.futunn.com/%22}; _ga_FZ1PVH4G8R=GS1.1.1716825937.4.1.1716826506.0.0.0; _ga_XECT8CPR37=GS1.1.1716825910.5.1.1716826507.57.0.0; _gat_gtag_UA_71722593_3=1; _gat_UA-71722593-2=1; _ga=GA1.1.1805342134.1715273013; _ga_K1RSSMGBHL=GS1.1.1716825938.4.1.1716826509.0.0.0; _uetsid=02bb59601c3311efb8b9e78a8b3bc8e6; _uetvid=dc29a000120011efa85b1b53db60ba28; _ga_NTZDYESDX1=GS1.2.1716825938.4.1.1716826510.60.0.0; _ga_EJJJZFNPTW=GS1.1.1716825910.5.1.1716826512.0.0.0; 0_us_req_section=2' \
//   -H 'DNT: 1' \
//   -H 'Referer: https://www.futunn.com/stock/MORF-US?global_content=%7B%22promote_id%22%3A13766,%22sub_promote_id%22%3A2%7D' \
//   -H 'Sec-Fetch-Dest: empty' \
//   -H 'Sec-Fetch-Mode: cors' \
//   -H 'Sec-Fetch-Site: same-origin' \
//   -H 'User-Agent: Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36 Edg/125.0.0.0' \
//   -H 'futu-x-csrf-token: PANsC570ivtIzYxothamiscZ' \
//   -H 'quote-token: e587388102' \
//   -H 'sec-ch-ua: "Microsoft Edge";v="125", "Chromium";v="125", "Not.A/Brand";v="24"' \
//   -H 'sec-ch-ua-mobile: ?0' \
//   -H 'sec-ch-ua-platform: "Linux"'