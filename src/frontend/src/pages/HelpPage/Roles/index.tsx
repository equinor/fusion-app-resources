import * as React from 'react';
import * as styles from './styles.less';
import EquinorAccount from '../svg/EquinorAccount';
import ContractorAccount from '../svg/ContractorAccount';
import SystemAccount from '../svg/SystemAccount';

type RoleDescriptionProps = {
    title: string;
    icon: React.ReactNode;
};

const RoleDescription: React.FC<RoleDescriptionProps> = ({ title, icon, children }) => {
    return (
        <div className={styles.roleDescription}>
            <div className={styles.header}>
                <div>{icon}</div>
                <span className={styles.title}>{title} </span>
            </div>
            <div className={styles.content}>{children}</div>
        </div>
    );
};

const Roles: React.FC = () => {
    return (
        <div className={styles.container}>
            <h2>Roles</h2>
            <RoleDescription title="Contractor and CR./CR" icon={<ContractorAccount />}>
                Any contractor user can create a request but only the External company rep. (CR.) or
                External contract responsible (CR), can approve, reject or delete one. If rejected
                it is stored in Completed requests.
            </RoleDescription>
            <RoleDescription title="Equinor CR./CR" icon={<EquinorAccount />}>
                When the request is approved by the CR. or CR, then an Equinor employee set as
                responsible for the contract, either Equinor company rep or Equinor contract
                responsible, can approve or reject the request. If rejected it is stored in
                Completed requests.
            </RoleDescription>
            <RoleDescription title="System account" icon={<SystemAccount />}>
                System account is a function that is automatically triggered to provision the
                request. This will send it to the Pro Org-chart where it will now be accessible. It
                will also be stored in the Completed requests.
            </RoleDescription>
        </div>
    );
};

export default Roles;
