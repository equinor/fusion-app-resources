
import styles from './styles.less';
import Personnel from '../../../../../../../models/Personnel';
import { FC } from 'react';

type PersonnelRequestProps = {
    person: Personnel;
};

const PersonnelRequest: FC<PersonnelRequestProps> = ({ person }) => {
    return (
        <div className={styles.personnelRequest}>
            <div className={styles.personName}>
                {person.firstName} {person.lastName}
            </div>
            <div className={styles.personMail}>
                <a href={'mailto:' + person.mail}>{person.mail}</a>
            </div>
        </div>
    );
};

export default PersonnelRequest;
