import com.pgssoft.meteopllibrary.*;

import javax.imageio.ImageIO;
import java.awt.image.BufferedImage;
import java.awt.image.Raster;
import java.io.File;
import java.io.IOException;
import java.io.PrintWriter;
import java.text.DecimalFormat;

/**
 * Created by Tomek on 2015-01-01.
 */

public class MeteoPLJavaSample implements Utils {


    public static void main(String[] args) throws IOException {

        new MeteoPLJavaSample().demo();
    }

    private void demo() throws IOException
    {
        BufferedImage img = ImageIO.read(new File("../../samples/mgram2.png"));
        int pixels[] = new int[img.getWidth()*img.getHeight()];

        int[] data = img.getRGB(0, 0, img.getWidth(), img.getHeight(), null, 0, img.getWidth());


        ModelUM modelUM = new ModelUM(data, this, true);
        double temp[] = modelUM.getSamples(modelUM.TYPE_TEMPERATURE);
        double tempPerc[] = modelUM.getSamples(modelUM.TYPE_TEMPERATURE_PERCEPTIBLE);
        double press[] = modelUM.getSamples(modelUM.TYPE_PRESSURE_HPA);

        PrintWriter writer = new PrintWriter("../../samples/mgram1javaoutput.txt", "UTF-8");
        DecimalFormat df = new DecimalFormat("0.00");
        for (int i=0; i<temp.length; i++)
        {
            writer.print(df.format(temp[i]));
            writer.print(";");
        }
        writer.println();
        for (int i=0; i<temp.length; i++)
        {
            writer.print(df.format(tempPerc[i]));
            writer.print(";");
        }
        writer.println();
        for (int i=0; i<temp.length; i++) {
            writer.print(df.format(press[i]));
            writer.print(";");
        }
        writer.println();
        writer.close();

    }

    @Override
    public long getTimestamp(int m, int d, double hour) {
        // TODO
        return 0;
    }

    @Override
    public void throwException(String description) {
        throw new RuntimeException(description);
    }
}
