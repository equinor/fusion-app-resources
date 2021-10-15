import { NavigationStructure, Chip } from '@equinor/fusion-components';
import { useHistory, combineUrls } from '@equinor/fusion';
import { History } from 'history';
import { IContractContext } from '../../../../contractContex';
import { ReactNode, useMemo, useState, useEffect } from 'react';

type NavStructureType = 'grouping' | 'section' | 'child';

const GeneralIcon = () => (
    <svg width="48" height="48" viewBox="0 0 48 48" fill="none" xmlns="http://www.w3.org/2000/svg">
        <path d="M23.333 20.6666H24.6663V21.9999H23.333V20.6666Z" fill="currentColor" />
        <path d="M23.333 23.3333H24.6663V27.3333H23.333V23.3333Z" fill="currentColor" />
        <path
            fillRule="evenodd"
            clipRule="evenodd"
            d="M17.333 23.9999C17.333 20.3199 20.3197 17.3333 23.9997 17.3333C27.6797 17.3333 30.6663 20.3199 30.6663 23.9999C30.6663 27.6799 27.6797 30.6666 23.9997 30.6666C20.3197 30.6666 17.333 27.6799 17.333 23.9999ZM18.6663 23.9999C18.6663 26.9399 21.0597 29.3333 23.9997 29.3333C26.9397 29.3333 29.333 26.9399 29.333 23.9999C29.333 21.0599 26.9397 18.6666 23.9997 18.6666C21.0597 18.6666 18.6663 21.0599 18.6663 23.9999Z"
            fill="currentColor"
        />
    </svg>
);

const ManagePersonnelIcon = () => (
    <svg width="48" height="48" viewBox="0 0 48 48" fill="none" xmlns="http://www.w3.org/2000/svg">
        <path
            fillRule="evenodd"
            clipRule="evenodd"
            d="M24 23.9999C25.2867 23.9999 26.3333 22.9533 26.3333 21.6666C26.3333 20.3799 25.2867 19.3333 24 19.3333C22.7133 19.3333 21.6667 20.3799 21.6667 21.6666C21.6667 22.9533 22.7133 23.9999 24 23.9999ZM19.3333 25.9999V23.9999H21.3333V22.6666H19.3333V20.6666H18V22.6666H16V23.9999H18V25.9999H19.3333ZM24 25.1666C22.44 25.1666 19.3333 25.9466 19.3333 27.4999V28.6666H28.6667V27.4999C28.6667 25.9466 25.56 25.1666 24 25.1666ZM24 26.4999C22.8067 26.4999 21.4533 26.9466 20.8933 27.3333H27.1067C26.5467 26.9466 25.1933 26.4999 24 26.4999ZM25 21.6666C25 21.1133 24.5533 20.6666 24 20.6666C23.4467 20.6666 23 21.1133 23 21.6666C23 22.2199 23.4467 22.6666 24 22.6666C24.5533 22.6666 25 22.2199 25 21.6666ZM27.3333 23.9999C28.62 23.9999 29.6667 22.9533 29.6667 21.6666C29.6667 20.3799 28.62 19.3333 27.3333 19.3333C27.1733 19.3333 27.0133 19.3466 26.86 19.3799C27.3667 20.0066 27.6667 20.7999 27.6667 21.6666C27.6667 22.5333 27.3533 23.3199 26.8467 23.9466C27.0067 23.9799 27.1667 23.9999 27.3333 23.9999ZM30 27.4999C30 26.5933 29.5467 25.8866 28.88 25.3466C30.3733 25.6599 32 26.3733 32 27.4999V28.6666H30V27.4999Z"
            fill="currentColor"
        />
    </svg>
);

const ManageMppIcon = () => (
    <svg width="48" height="48" viewBox="0 0 48 48" fill="none" xmlns="http://www.w3.org/2000/svg">
        <path d="M18 24.6667H19.3333V23.3334H18V24.6667Z" fill="currentColor" />
        <path d="M18 27.3334H19.3333V26.0001H18V27.3334Z" fill="currentColor" />
        <path d="M18 22.0001H19.3333V20.6667H18V22.0001Z" fill="currentColor" />
        <path d="M20.6667 24.6667H30V23.3334H20.6667V24.6667Z" fill="currentColor" />
        <path d="M20.6667 27.3334H30V26.0001H20.6667V27.3334Z" fill="currentColor" />
        <path d="M20.6667 20.6667V22.0001H30V20.6667H20.6667Z" fill="currentColor" />
    </svg>
);

const createContractPath = (history: History, contractId: string, path: string) => {
    const base = history.location.pathname.split('/' + contractId)[0];
    return combineUrls(base, contractId, path);
};

const createNavItem = (
    history: History,
    contractId: string,
    title: string,
    path: string,
    type: NavStructureType,
    customid?: string,
    icon?: ReactNode,
    aside?: ReactNode,
): NavigationStructure => ({
    id: customid ?? title,
    title,
    type,
    isActive: history.location.pathname === createContractPath(history, contractId, path),
    onClick: () => history.push(createContractPath(history, contractId, path)),
    icon,
    aside,
});

const getNavigationStructure = (
    history: History,
    contractId: string,
    provisioningComponent: ReactNode
): NavigationStructure[] => {
    return [
        createNavItem(history, contractId, 'General', '', 'grouping', 'general-tab',<GeneralIcon />),

        {
            id: 'manage-personnel',
            title: 'Manage personnel',
            type: 'grouping',
            icon: <ManagePersonnelIcon />,
            onClick: () =>
                history.push(createContractPath(history, contractId, 'manage-personnel')),
            isOpen: true,
            navigationChildren: [
                createNavItem(
                    history,
                    contractId,
                    'Contract personnel',
                    'manage-personnel',
                    'child'
                ),
                createNavItem(
                    history,
                    contractId,
                    'Preferred contact mail',
                    'manage-personnel-mails',
                    'child'
                ),
            ],
        },

        {
            id: 'manage-mpp',
            title: 'Manage MPP',
            type: 'grouping',
            isOpen: true,
            icon: <ManageMppIcon />,
            onClick: () => history.push(createContractPath(history, contractId, 'actual-mpp')),
            navigationChildren: [
                createNavItem(history, contractId, 'Actual MPP', 'actual-mpp', 'child'),
                createNavItem(history, contractId, 'Active requests', 'active-requests', 'child'),
                createNavItem(
                    history,
                    contractId,
                    'Provisioning requests',
                    'provisioning-requests',
                    'child',
                    undefined,
                    provisioningComponent
                ),
                createNavItem(
                    history,
                    contractId,
                    'Completed requests',
                    'completed-requests',
                    'child'
                ),
            ],
        },
    ];
};

const useNavigationStructure = (contractId: string, contractContext: IContractContext) => {
    const history = useHistory();

    const provisioningRequests = contractContext.contractState.completedRequests.data;
    const provisioning = provisioningRequests.filter(
        (r) => r.provisioningStatus?.state === 'NotProvisioned' && r.state === 'ApprovedByCompany'
    ).length;
    const failedProvisioning = provisioningRequests.filter(
        (r) => r.provisioningStatus?.state === 'Error' && r.state === 'ApprovedByCompany'
    ).length;
    const provisioningComponent = useMemo(() => {
        if (provisioning <= 0 && failedProvisioning <= 0) {
            return null;
        }
        return (
            <div>
                {provisioning > 0 && <Chip primary title={provisioning.toString()} />}
                {failedProvisioning > 0 && <Chip secondary title={failedProvisioning.toString()} />}
            </div>
        );
    }, [provisioning, failedProvisioning]);

    const [structure, setStructure] = useState<NavigationStructure[]>(
        getNavigationStructure(history, contractId, provisioningComponent)
    );

    useEffect(() => {
        setStructure(getNavigationStructure(history, contractId, provisioningComponent));
    }, [contractId, history.location.pathname, provisioning, failedProvisioning]);

    return { structure, setStructure };
};

export default useNavigationStructure;
