import * as React from 'react';
import { DataTableColumn } from "@equinor/fusion-components";
import Contract from '../../../../models/contract';
import ContractLinkColumn from './components/ContractLinkColumn';
import PositionColumn from './components/PositionColumn';

const createColumns = (): DataTableColumn<Contract>[] => [
    {
        accessor: 'contractNumber',
        key: 'number',
        label: 'Contract no.',
        sortable: true,
        component: ({ item }) => (
            <ContractLinkColumn contractId={item.id}>{item.contractNumber}</ContractLinkColumn>
        ),
    },
    {
        accessor: 'name',
        key: 'name',
        label: 'Name',
        sortable: true,
        component: ({ item }) => (
            <ContractLinkColumn contractId={item.id}>{item.name}</ContractLinkColumn>
        ),
    },
    {
        accessor: 'companyRepPositionId',
        key: 'companyRepPositionId',
        label: 'Equinor company rep',
        component: ({ item }) => <PositionColumn positionId={item.companyRepPositionId} />,
    },
    {
        accessor: 'contractResponsiblePositionId',
        key: 'contractResponsiblePositionId',
        label: 'Equinor contract rep',
        component: ({ item }) => <PositionColumn positionId={item.contractResponsiblePositionId} />,
    },
    {
        accessor: contract => contract.company?.id || 'no company',
        key: 'company',
        label: 'Company',
        sortable: true,
    },
    {
        accessor: 'externalContractResponsiblePositionId',
        key: 'externalContractResponsiblePositionId',
        label: 'External company rep',
        component: ({ item }) => <PositionColumn positionId={item.externalContractResponsiblePositionId} />,
    },
    {
        accessor: 'externalCompanyRepPositionId',
        key: 'externalCompanyRepPositionId',
        label: 'External contract rep',
        component: ({ item }) => <PositionColumn positionId={item.externalCompanyRepPositionId} />,
    },
];

export default createColumns;