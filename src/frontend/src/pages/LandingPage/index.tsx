import * as styles from './styles.less';
import { SortIcon } from '@equinor/fusion-components';

const LandingPage = () => {
    return (
        <h2 className={styles.chooseProject}>
            <SortIcon direction="asc" />
            <br />
            Start by choosing project
        </h2>
    );
};

export default LandingPage;
