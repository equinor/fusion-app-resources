import { useState, useEffect } from "react";
import { NavigationStructure } from "@equinor/fusion-components";
import { useHistory, combineUrls } from '@equinor/fusion';
import { History } from "history";

const createContractPath = (history: History, contractId: string, path: string) => {
    const base = history.location.pathname.split("/" + contractId)[0];
    return combineUrls(base, contractId, path);
};

const createNavItem = (history: History, contractId: string, title: string, path: string): NavigationStructure => ({
    id: title,
    title,
    type: 'section',
    isActive: history.location.pathname === createContractPath(history, contractId, path),
    onClick: () => history.push(createContractPath(history, contractId, path)),
});

const getNavigationStructure = (history: History, contractId: string): NavigationStructure[] => {
    return [
        createNavItem(history, contractId, "General", ""),
        createNavItem(history, contractId, "Manage personnel", "personnel"),
        {
            id: 'manage-mpp',
            title: 'Manage MPP',
            type: 'grouping',
            isOpen: true,
            navigationChildren: [
                createNavItem(history, contractId, "Actual MPP", "actual-mpp"),
                createNavItem(history, contractId, "Active requests", "active-requests"),
                createNavItem(history, contractId, "Log", "Log"),
            ]
        },
    ];
}

const useNavigationStructure = (contractId: string) => {
    const history = useHistory();
    const [structure, setStructure] = useState<NavigationStructure[]>(getNavigationStructure(history, contractId));

    useEffect(() => {
        setStructure(getNavigationStructure(history, contractId));
    }, [contractId, history.location.pathname]);

    return { structure, setStructure };
};

export default useNavigationStructure;