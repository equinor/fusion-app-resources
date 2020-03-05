import * as React from 'react';
import { DataTableColumn, styling } from "@equinor/fusion-components";
import Contract from '../../../../models/contract';
import ContractLinkColumn from './components/ContractLinkColumn';
import PositionColumn from '../../components/PositionColumn';

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
        width: styling.grid(20),
        component: ({ item }) => (
            <ContractLinkColumn contractId={item.id}>{item.name}</ContractLinkColumn>
        ),
    },
    {
        accessor: 'companyRepPositionId',
        key: 'companyRepPositionId',
        label: 'Equinor company rep',
        component: ({ item }) => <PositionColumn position={item.companyRep} />,
    },
    {
        accessor: 'contractResponsiblePositionId',
        key: 'contractResponsiblePositionId',
        label: 'Equinor contract rep',
        component: ({ item }) => <PositionColumn position={item.contractResponsible} />,
    },
    {
        accessor: contract => contract.company?.name || 'no company',
        key: 'company',
        label: 'Company',
        sortable: true,
    },
    {
        accessor: 'externalContractResponsiblePositionId',
        key: 'externalContractResponsiblePositionId',
        label: 'External company rep',
        component: ({ item }) => <PositionColumn position={item.externalContractResponsible} />,
    },
    {
        accessor: 'externalCompanyRepPositionId',
        key: 'externalCompanyRepPositionId',
        label: 'External contract rep',
        component: ({ item }) => <PositionColumn position={item.externalCompanyRep} />,
    },
];

export default createColumns;