import { useState, useEffect } from 'react';
import { NavigationStructure } from '@equinor/fusion-components';
import { useHistory, combineUrls } from '@equinor/fusion';
import { History } from 'history';

type NavStructureType = 'grouping' | 'section' | 'child';

const createContractPath = (history: History, contractId: string, path: string) => {
    const base = history.location.pathname.split('/' + contractId)[0];
    return combineUrls(base, contractId, path);
};

const createNavItem = (
    history: History,
    contractId: string,
    title: string,
    path: string,
    type: NavStructureType
): NavigationStructure => ({
    id: title,
    title,
    type,
    isActive: history.location.pathname === createContractPath(history, contractId, path),
    onClick: () => history.push(createContractPath(history, contractId, path)),
});

const getNavigationStructure = (history: History, contractId: string): NavigationStructure[] => {
    return [
        createNavItem(history, contractId, 'General', '', 'grouping'),
        createNavItem(history, contractId, 'Manage personnel', 'managepersonnel', 'grouping'),
        {
            id: 'manage-mpp',
            title: 'Manage MPP',
            type: 'grouping',
            isOpen: true,
            onClick: () => history.push(createContractPath(history, contractId, 'actual-mpp')),
            navigationChildren: [
                createNavItem(history, contractId, 'Actual MPP', 'actual-mpp', 'child'),
                createNavItem(history, contractId, 'Active requests', 'active-requests', 'child'),
                createNavItem(history, contractId, 'Log', 'Log', 'child'),
            ],
        },
    ];
};

const useNavigationStructure = (contractId: string) => {
    const history = useHistory();
    const [structure, setStructure] = useState<NavigationStructure[]>(
        getNavigationStructure(history, contractId)
    );

    useEffect(() => {
        setStructure(getNavigationStructure(history, contractId));
    }, [contractId, history.location.pathname]);

    return { structure, setStructure };
};

export default useNavigationStructure;
