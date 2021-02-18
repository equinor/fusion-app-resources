import { useState, useEffect } from 'react';
import PersonnelRequest from '../../../../../../../models/PersonnelRequest';
import { useHistory } from '@equinor/fusion';
import { parseQueryString } from '../../../../../../../api/utils';

export default (requests: PersonnelRequest[] | null) => {
    const history = useHistory();
    const [currentRequest, setCurrentRequest] = useState<PersonnelRequest | null>();

    useEffect(() => {
        if (!requests) {
            return;
        }
        const params = parseQueryString(history.location.search);
        const selectedRequest = requests.find(r => r.id === params.requestId);
        setCurrentRequest(selectedRequest || null);
    }, [history.location.search, requests]);

    useEffect(() => {
        if (currentRequest === null) {
            history.push({
                pathname: history.location.pathname,
                search: '',
            });
        }
    }, [currentRequest]);

    return { currentRequest: currentRequest || null, setCurrentRequest };
};
